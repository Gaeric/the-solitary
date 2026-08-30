using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 挖掘（character.org 蓝卡 #11）：1 费攻击，打出后消耗。
// 造成 6 点伤害（升级后 9 点）；抽牌直到抽到一张附魔牌。
// 抽牌直到逻辑参考原版 劫掠 Pillage（do-while 逐张抽，附带手牌上限保护）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class EnchantDig : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（攻击）。
	private const CardType CardKind = CardType.Attack;
	// 卡牌稀有度（蓝卡 = Uncommon）。
	private const CardRarity CardRarityValue = CardRarity.Uncommon;
	// 目标类型（任意敌人）。
	private const TargetType CardTarget = TargetType.AnyEnemy;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public EnchantDig()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/EnchantDig.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 打出后消耗。
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

	// 基础数值：伤害 6（升级后 9），绑定 {Damage:diff()} 占位符。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(6m, ValueProp.Move)
	];

	// 打出时：先造成伤害，再抽牌直到抽到一张附魔牌。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target);

		// 1. 造成 6 点伤害。
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.Execute(choiceContext);

		// 2. 抽牌直到抽到一张附魔牌（参考原版 劫掠 Pillage 的 do-while 逐张抽循环；
		//    附带手牌上限保护；抽牌堆/弃牌堆都抽空后 Draw 返回 null 则停止）。
		CardModel? drawn;
		do
		{
			drawn = await CardPileCmd.Draw(choiceContext, Owner);
		}
		while (drawn != null && drawn.Enchantment == null
		       && CardPile.GetCards(Owner, PileType.Hand).Count() < CardPile.MaxCardsInHand);
	}

	// 升级：伤害 6 -> 9（参考劫掠 Pillage 的升级方式）。
	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(3m);
	}
}
