using System.Reflection;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Patching.Core;
using TheSolitary.Cards;
using TheSolitary.Characters;
using TheSolitary.Patches;
using TheSolitary.Relics;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace TheSolitary;

[ModInitializer(nameof(Initialize))]
public partial class Entry
{
    // ModId 需要和 TheSolitary.json 里的 id 保持一致。
    // res://TheSolitary/... 里的 TheSolitary 是 PCK 资源目录，不是 C# namespace。
    public const string ModId = "TheSolitary";
    public const string ResPath = $"res://{ModId}";

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();

        // 以下示例默认已经在 Entry.Initialize() 中调用了
        // RitsuLibFramework.EnsureGodotScriptsRegistered(...) 和
        // ModTypeDiscoveryHub.RegisterModAssembly(...)，否则自动注册不会生效。
        //
        // Godot C# 脚本注册只负责让 pck 中的脚本类型能被 Godot 找到。
        // 这一步和 RitsuLib 的内容自动注册不是同一件事，两个都需要保留。
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);

        // 自动注册扫描会读取当前程序集里的 RegisterCard/RegisterRelic 等 attribute。
        // 新增内容类后，只要 attribute 写对，通常不需要在入口里手动逐个注册。
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

        // 事件关联：古老牙齿 ArchaicTooth 把初始卡 唤醒 Sacrifice 升级成先古卡 复苏 Resurgence。
        // 映射 ID 延迟解析，放在内容注册之后即可。
        RitsuLibFramework.RegisterArchaicToothTranscendenceMapping<Sacrifice, Resurgence>(ModId);

        // 事件关联：尘封魔典 DustyTome 优先选择角色先古卡池中的 黏糊魔典 StickyGrimoire
        // （复苏 Resurgence 是古老牙齿的先古升级牌，会被 DustyTome 自动排除）。
        RitsuLibFramework.RegisterDustyTomeCard<TheSolitaryCharacter, StickyGrimoire>(ModId);

        // 事件关联：欧洛巴斯之触 TouchOfOrobas 把初始遗物 迅捷回路 SwiftCircuit 精炼成 极速回路 RapidCircuit。
        RitsuLibFramework.RegisterTouchOfOrobasRefinementMapping<SwiftCircuit, RapidCircuit>(ModId);

        // 附魔共鸣 / 附魔守护：游戏没有"获得附魔后"的钩子，需通过 Harmony 补丁拦截 CardCmd.Enchant。
        // 用 RitsuLib 的补丁 API 注册（非关键补丁，游戏更新签名变化时仅该功能失效）。
        var patcher = RitsuLibFramework.CreatePatcher(ModId, "after-enchant", "After Enchant Effects", LogType.Generic);
        patcher.RegisterPatch<AfterEnchantPatch>();
        patcher.PatchAll();

        // 余烬附魔降费记录：原版 TezcatarasEmber.OnEnchant 用 EnergyCost.UpgradeBy 把基础费用
        // 永久改写成 0、清除附魔不会还原（无 OnUnenchant 钩子）。该补丁在施加时把"附魔前费用"
        // 写入附魔 Props，供 EnchantHelpers 在清除/交换余烬时恢复（修复移除余烬后费用不回到原值）。
        var emberCostPatcher = RitsuLibFramework.CreatePatcher(ModId, "tezcataras-ember-cost-record", "Tezcataras Ember Cost Record", LogType.Generic);
        emberCostPatcher.RegisterPatch<TezcatarasEmberCostRecordPatch>();
        emberCostPatcher.PatchAll();

        // 附魔改写关键词记录：附魔的 OnEnchant 会永久改写卡牌关键词（灵魂之力 SoulsPower 移除消耗，
        // 黏糊/稳定/御准/余烬添加关键词），清除附魔同样不会还原。该补丁在 EnchantmentModel.ModifyCard
        // （OnEnchant 唯一调用点）前把"附魔前的本地关键词集合"写入附魔 Props，
        // 供 EnchantHelpers 在清除附魔时按快照还原（修复：灵魂之力的附魔被移走后消耗词条不恢复）。
        var enchantKeywordPatcher = RitsuLibFramework.CreatePatcher(ModId, "enchant-keyword-record", "Enchant Keyword Record", LogType.Generic);
        enchantKeywordPatcher.RegisterPatch<EnchantKeywordRecordPatch>();
        enchantKeywordPatcher.PatchAll();

        // 墨影（Inky）目标补丁：原版 Inky.OnPlay 只认 cardPlay.Target，随机多段攻击牌（RandomEnemy，
        // 如光子映射）不选目标 → target 为 null → PowerCmd.Apply 抛 NRE；且原版每次打牌最多只施加 1 层，
        // 无法表达"每命中一次加一层虚弱"。两个补丁类配合：InkyHitRecordPatch 登记每次命中，
        // InkyTargetPatch 结算时按命中次数逐层施加（同一敌人多段命中会叠多层）。
        var inkyPatcher = RitsuLibFramework.CreatePatcher(ModId, "inky-target", "Inky Target Guard", LogType.Generic);
        inkyPatcher.RegisterPatch<InkyHitRecordPatch>();
        inkyPatcher.RegisterPatch<InkyTargetPatch>();
        inkyPatcher.PatchAll();

        Logger.Info("TheSolitary initialized.");
    }
}
