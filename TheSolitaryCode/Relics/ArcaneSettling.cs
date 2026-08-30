using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Relics;

// 元能沉淀（罕见遗物）：每回合首次生成附魔时，获得 4 点格挡。
// 游戏没有"获得附魔后"的事件钩子（CardCmd.Enchant 不触发任何 Hook），
// 实际触发由 TheSolitaryCode/Patches/AfterEnchantPatch.cs 在 CardCmd.Enchant 成功后派发。
[RegisterRelic(typeof(TheSolitaryRelicPool))]
public sealed class ArcaneSettling : ModRelicTemplate
{
    // 每回合首次生成附魔时获得的格挡。
    private const decimal BlockAmount = 4m;

    // 本回合是否已经触发过"首次生成附魔"。
    private bool _triggeredThisTurn;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    // 基础数值：格挡（绑定 {Block:diff()} 占位符）。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(BlockAmount, ValueProp.Unpowered)
    ];

    private bool TriggeredThisTurn
    {
        get => _triggeredThisTurn;
        set
        {
            AssertMutable();
            _triggeredThisTurn = value;
        }
    }

    // 图片资源统一放在 AssetProfile 里配置（三个路径先指向同一张图）。
    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png");

    // 回合开始时重置"本回合已触发"标志。
    // 注意：不能用 AfterPlayerTurnStart —— 它在初始手牌抽取之后才派发，
    // 会把迅捷回路在抽牌时触发的附魔误算作下一回合（导致同回合触发两次）。
    // AfterSideTurnStart 在初始抽牌之前派发（参考原版信使/信封刀 LetterOpener 的回合重置写法）。
    public override Task AfterSideTurnStart(
        CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (participants.Contains(base.Owner.Creature))
        {
            TriggeredThisTurn = false;
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// 由 AfterEnchantPatch 在 CardCmd.Enchant 成功后调用：本回合首次生成附魔时获得格挡。
    /// </summary>
    public async Task OnEnchantTriggered(PlayerChoiceContext choiceContext, Creature owner)
    {
        if (TriggeredThisTurn)
        {
            return;
        }
        TriggeredThisTurn = true;
        Flash();
        // ValueProp.Unpowered：来自遗物的格挡不享受力量/敏捷加成（与原版遗物一致）。
        await CreatureCmd.GainBlock(owner, DynamicVars.Block.BaseValue, ValueProp.Unpowered, null);
    }
}
