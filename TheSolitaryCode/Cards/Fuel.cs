using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 燃料（character.org 金卡 #7）：1 费技能，消耗。
// 消耗手牌中所有附魔牌；每消耗一张，获得 1 点能量并抽 1 张牌。
// 消耗手牌参考原版 FiendFire，能量/抽牌参考原版 Adrenaline。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Fuel : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（金卡 = Rare）。
	private const CardRarity CardRarityValue = CardRarity.Rare;
	// 目标类型（Self）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public Fuel()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Fuel.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 打出后消耗（设计文档的末尾“消耗”）。
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

	// 基础数值：每消耗一张附魔牌获得 1 能量、抽 1 张牌（绑定 {Energy:energyIcons()} / {Cards:diff()}）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new EnergyVar(1),
		new CardsVar(1)
	];

	// 打出时：先快照手牌中的附魔牌，再逐个消耗，每张 +1 能量、抽 1 张牌。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

		// 快照：消耗会改动牌堆集合，先取出要消耗的牌。
		List<CardModel> enchantedInHand = Owner.PlayerCombatState!.Hand.Cards
			.Where(card => card.Enchantment != null)
			.ToList();

		foreach (CardModel card in enchantedInHand)
		{
			await CardCmd.Exhaust(choiceContext, card);
			await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
			await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
		}
	}

	// 升级：费用 1 -> 0。
	protected override void OnUpgrade()
	{
		base.EnergyCost.UpgradeBy(-1);
	}
}
