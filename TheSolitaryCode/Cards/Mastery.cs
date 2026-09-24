using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Mastery : ModCardTemplate
{
	private const int BaseEnergyCost = 2;
	private const CardType CardKind = CardType.Skill;
	private const CardRarity CardRarityValue = CardRarity.Rare;
	private const TargetType CardTarget = TargetType.Self;
	private const bool ShowInCardLibrary = true;

	public Mastery()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Innate, CardKeyword.Exhaust];

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		HoverTipFactory.FromEnchantment<Spiral>();

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

		foreach (CardModel card in EnchantHelpers.GetAllCombatPileCards(Owner))
		{
			if (!CanEnchantSpiral(card))
			{
				continue;
			}

			CardCmd.Enchant<Spiral>(card, 1m);
			CardCmd.ApplyKeyword(card, CardKeyword.Exhaust);
		}
	}

	protected override void OnUpgrade()
	{
		base.EnergyCost.UpgradeBy(-1);
	}

	private static bool CanEnchantSpiral(CardModel card)
	{
		return ModelDb.Enchantment<Spiral>().ToMutable().CanEnchant(card);
	}
}
