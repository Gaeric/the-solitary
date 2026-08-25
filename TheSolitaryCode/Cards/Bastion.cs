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

// 固元御甲（character.org 蓝卡 #30）：1 费技能，消耗。
// 获得与当前手牌中附魔牌数等量的覆甲；升级后额外 +3。
// 计算数值参考原版 Stack（CalculationBase 升级模式），覆甲施加参考原版 岩石铠甲 StoneArmor。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Bastion : ModCardTemplate
{
	// 计算覆甲层数的 DynamicVar 键名（绑定 {CalculatedPlating:diff()} 占位符）。
	private const string CalculatedPlatingKey = "CalculatedPlating";

	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（蓝卡 = Uncommon）。
	private const CardRarity CardRarityValue = CardRarity.Uncommon;
	// 目标类型（自身，无手动选敌）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public Bastion()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Bastion.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 打出后消耗。
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

	// 悬停提示：展示覆甲的机制说明。
	// 注意：RitsuLib 的 ModCardTemplate 已把 ExtraHoverTips sealed，悬停提示统一覆写 AdditionalHoverTips。
	protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<PlatingPower>()];

	// 基础数值：计算覆甲层数 = CalculationBase(0，升级后 3) + 手牌中附魔牌数量 × 1。
	// 参考原版 Stack：升级叠加上 CalculationBase（覆盖基础值），而不是覆写计算式本身。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CalculationBaseVar(0m),
		new CalculationExtraVar(1m),
		new CalculatedVar(CalculatedPlatingKey).WithMultiplier(static (card, _) =>
			card.Owner.PlayerCombatState!.Hand.Cards.Count(c => c.Enchantment != null))
	];

	// 打出时：按当前计算值给自身施加覆甲（参考原版 Stack.OnPlay / StoneArmor.OnPlay）。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		decimal plating = ((CalculatedVar)DynamicVars[CalculatedPlatingKey]).Calculate(cardPlay.Target);
		await PowerCmd.Apply<PlatingPower>(choiceContext, Owner.Creature, plating, Owner.Creature, this);
	}

	// 升级：覆甲基础值 0 -> 3（仍加上手牌中附魔牌数量）。
	protected override void OnUpgrade()
	{
		DynamicVars.CalculationBase.UpgradeValueBy(3m);
	}
}
