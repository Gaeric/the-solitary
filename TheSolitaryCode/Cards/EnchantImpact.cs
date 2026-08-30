using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 移位（character.org 白卡 #1）：1 费攻击。
// 造成 6 点伤害，抽取手牌中附魔数量张牌（参考原版 编译冲击 CompileDriver 的计算抽牌机制）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class EnchantImpact : ModCardTemplate
{
	// 计算抽牌数对应的 DynamicVar 键名。
	private const string CalculatedCardsKey = "CalculatedCards";

	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（攻击）。
	private const CardType CardKind = CardType.Attack;
	// 卡牌稀有度（白卡 = Common）。
	private const CardRarity CardRarityValue = CardRarity.Common;
	// 目标类型（任意敌人）。
	private const TargetType CardTarget = TargetType.AnyEnemy;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public EnchantImpact()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/EnchantImpact.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：伤害 + 计算抽牌数。
	// 抽牌数 = CalculationBase(0) + CalculationExtra(1) * 手牌中附魔牌的数量（参考 CompileDriver 的 CalculatedVar）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(6m, ValueProp.Move),
		new CalculationBaseVar(0m),
		new CalculationExtraVar(1m),
		new CalculatedVar(CalculatedCardsKey).WithMultiplier(static (card, _) =>
			card.Owner.PlayerCombatState!.Hand.Cards.Count(c => c.Enchantment != null))
	];

	// 打出时：先造成 6 点伤害，再按手牌中附魔牌数量抽牌（参考 CompileDriver.OnPlay）。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target);

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.Execute(choiceContext);

		await CardPileCmd.Draw(choiceContext,
			((CalculatedVar)DynamicVars[CalculatedCardsKey]).Calculate(cardPlay.Target), Owner);
	}

	// 升级：伤害 6 -> 9。
	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(3m);
	}
}
