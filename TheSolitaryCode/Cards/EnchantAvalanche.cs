using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 附魔雪崩（character.org 白卡 #17）：3 费攻击。
// 造成 20 点伤害（升级后 25 点）。每打出一张附魔牌，本场战斗中此牌耗能 -1。
// 费用递减参考原版 女妖之嚎 BansheesCry（AfterCardPlayed + EnergyCost.AddThisCombat）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class EnchantAvalanche : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 3;
	// 卡牌类型（攻击）。
	private const CardType CardKind = CardType.Attack;
	// 卡牌稀有度（白卡 = Common）。
	private const CardRarity CardRarityValue = CardRarity.Common;
	// 目标类型（任意敌人）。
	private const TargetType CardTarget = TargetType.AnyEnemy;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public EnchantAvalanche()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/EnchantAvalanche.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：伤害 20（绑定 {Damage:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(20m, ValueProp.Move)
	];

	// 打出时：造成 20 点伤害。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target);

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.Execute(choiceContext);
	}

	// 每打出一张附魔牌，本场战斗中此牌耗能 -1（参考原版 BansheesCry.AfterCardPlayed）。
	// 卡片位于战斗牌堆（手牌/抽牌堆/弃牌堆）时都会收到该钩子，因此尚未抽到手也能累计减费。
	public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner != Owner)
		{
			return Task.CompletedTask;
		}
		if (cardPlay.Card.Enchantment == null)
		{
			return Task.CompletedTask;
		}
		EnergyCost.AddThisCombat(-1);
		return Task.CompletedTask;
	}

	// 升级：伤害 20 -> 25。
	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(5m);
	}
}
