using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.CardSelection;

namespace TheSolitary.Cards;

// RegisterCard adds this card to the TheSolitary card pool (auto-registered by RitsuLib).
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class SwapEnchantments : ModCardTemplate
{
	// Base energy cost (upgraded to 0).
	private const int BaseEnergyCost = 1;
	// Card type (a Skill since it grants Block and manipulates the hand).
	private const CardType CardKind = CardType.Skill;
	// Card rarity.
	private const CardRarity CardRarityValue = CardRarity.Uncommon;
	// Target type (Self: the effect targets the player's own hand).
	private const TargetType CardTarget = TargetType.Self;
	// Whether to show the card in the card library.
	private const bool ShowInCardLibrary = true;

	public SwapEnchantments()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// Card art resource; the file name matches the class name in TheSolitary/images/cards/.
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// The card grants Block, so the game shows the Block hover tip and treats it as a block card.
	public override bool GainsBlock => true;

	// Base card values. BlockVar binds to the {Block:diff()} placeholder in the localized text.
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(5m, ValueProp.Move)
	];

	/// <summary>
	/// Gain 5 Block, then swap the enchantments of two cards in your hand (character.org, blue card #3).
	///
	/// Each selected card is first reset to its un-enchanted base state (via Transform), then the other card's
	/// enchantment is applied to it as a fresh instance: the enchantment's Id, Props and Amount are preserved, but
	/// its runtime state is reset to its initial value (Status back to Normal, one-shot flags cleared), so e.g. a
	/// Vigorous or Glam that was already used is "recharged" after being swapped. A card that had no enchantment
	/// simply gains the other card's enchantment.
	/// </summary>
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		// Design doc (character.org, blue card #3): gain 5 Block first.
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

		// Choose exactly two cards from your hand. Enchanted cards glow to make the swap easier to plan.
		List<CardModel> selection = (await CardSelectCmd.FromHand(
			prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 2)
			{
				ShouldGlowGold = card => card.Enchantment != null
			},
			context: choiceContext,
			player: base.Owner,
			filter: null,
			source: this)).ToList();

		if (selection.Count < 2)
		{
			return;
		}

		CardModel first = selection[0];
		CardModel second = selection[1];

		// Snapshot both enchantments as fresh instances (initial runtime state) BEFORE resetting the cards,
		// since resetting discards the originals.
		EnchantmentModel? firstEnchantment = RebuildEnchantment(first.Enchantment);
		EnchantmentModel? secondEnchantment = RebuildEnchantment(second.Enchantment);

		// Reset both cards to their un-enchanted base state, preserving their upgrade level.
		CardModel newFirst = ResetToUnEnchanted(first);
		CardModel newSecond = ResetToUnEnchanted(second);

		// Swap both cards in place (same pile, same positions).
		await CardCmd.Transform(
			new CardTransformation[2]
			{
				new CardTransformation(first, newFirst),
				new CardTransformation(second, newSecond)
			},
			null);

		// Apply the swapped enchantments. This is unconditional (mirroring how the game re-applies enchantments
		// on load): the enchantment is attached even if CanEnchant would normally reject the target card.
		if (secondEnchantment != null)
		{
			ApplyEnchantment(newFirst, secondEnchantment);
		}
		if (firstEnchantment != null)
		{
			ApplyEnchantment(newSecond, firstEnchantment);
		}
	}

	// Upgraded: the card becomes free (Block and the swap effect keep their base values).
	protected override void OnUpgrade()
	{
		base.EnergyCost.UpgradeBy(-1);
	}

	/// <summary>
	/// Rebuild an enchantment from its serialized form so the swapped copy has a fresh initial runtime state
	/// (Status reset to Normal, one-shot flags cleared) while keeping its Id, Props and Amount.
	/// </summary>
	private static EnchantmentModel? RebuildEnchantment(EnchantmentModel? enchantment)
	{
		if (enchantment == null)
		{
			return null;
		}
		return EnchantmentModel.FromSerializable(enchantment.ToSerializable());
	}

	/// <summary>
	/// Apply an enchantment to a card using the internal path, mirroring the steps CardCmd.Enchant takes after
	/// EnchantInternal (but bypassing the CanEnchant check, since a swap is unconditional).
	/// </summary>
	private static void ApplyEnchantment(CardModel card, EnchantmentModel enchantment)
	{
		card.EnchantInternal(enchantment, enchantment.Amount);
		enchantment.ModifyCard();
		card.FinalizeUpgradeInternal();
	}

	/// <summary>
	/// Create a fresh base copy of the card (no enchantment, no OnEnchant body changes, no affliction) preserving
	/// its upgrade level. CardCmd.Transform later inserts the copy into the original card's pile.
	/// </summary>
	private CardModel ResetToUnEnchanted(CardModel original)
	{
		CardModel replacement = original.CardScope!.CreateCard(original.CanonicalInstance, original.Owner);
		replacement.FloorAddedToDeck = original.FloorAddedToDeck;

		// Mirror how CardModel.FromSerializable re-applies upgrade levels to a fresh card.
		for (int i = 0; i < original.CurrentUpgradeLevel; i++)
		{
			replacement.UpgradeInternal();
			replacement.FinalizeUpgradeInternal();
		}
		return replacement;
	}
}

