using System.Reflection;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Patching.Core;
using TheSolitary.Patches;
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

        Logger.Info("TheSolitary initialized.");
    }
}
