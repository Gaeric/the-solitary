using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 破妄（character.org 金卡 #11）：1 费能力牌。
// 移除你身上的全部负面效果，获得 5 层覆甲（升级后 7 层）。
// 作为能力牌，其持续 Power 即覆甲（PlatingPower：回合结束获得格挡、回合开始减 1 层）；
// 负面效果移除是一次性效果，通过共享工具 DebuffHelpers.RemoveAllDebuffs 完成。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Cleanse : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（能力）。
	private const CardType CardKind = CardType.Power;
	// 卡牌稀有度（金卡 = Rare）。
	private const CardRarity CardRarityValue = CardRarity.Rare;
	// 目标类型（自身，无手动选敌）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public Cleanse()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Cleanse.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 悬停提示：展示覆甲的机制说明。
	// 注意：RitsuLib 的 ModCardTemplate 已把 ExtraHoverTips sealed，悬停提示统一覆写 AdditionalHoverTips。
	protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<PlatingPower>()];

	// 基础数值：获得的覆甲层数（绑定 {PlatingPower:diff()} 占位符，与 PowerVar 键名一致）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<PlatingPower>(5m)
	];

	// 打出时：先移除自己身上的全部负面效果，再获得覆甲。
	// 覆甲作为能力牌的持续 Power 施加（PlatingPower），无需额外 Exhaust（能力牌打出后自动进入能力槽）。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await DebuffHelpers.RemoveAllDebuffs(Owner.Creature);
		await PowerCmd.Apply<PlatingPower>(choiceContext, Owner.Creature,
			DynamicVars["PlatingPower"].BaseValue, Owner.Creature, this);
	}

	// 升级：覆甲 5 -> 7（费用保持 1）。
	protected override void OnUpgrade()
	{
		DynamicVars["PlatingPower"].UpgradeValueBy(2m);
	}
}
