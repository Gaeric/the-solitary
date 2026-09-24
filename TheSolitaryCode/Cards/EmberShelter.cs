using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class EmberShelter : ModCardTemplate
{
	private const int BaseEnergyCost = 1;
	private const CardType CardKind = CardType.Skill;
	private const CardRarity CardRarityValue = CardRarity.Rare;
	private const TargetType CardTarget = TargetType.Self;
	private const bool ShowInCardLibrary = true;

	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

	public EmberShelter()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	public override bool GainsBlock => true;
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(8m, ValueProp.Move)
	];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		HoverTipFactory.FromEnchantment<TezcatarasEmber>();

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

		CardModel? target = (await CardSelectCmd.FromHand(
			context: choiceContext,
			player: Owner,
			prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 1),
			filter: CanEnchantEmber,
			source: this)).FirstOrDefault();

		if (target != null)
		{
			CardCmd.Enchant<TezcatarasEmber>(target, 1m);
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(3m);
	}

	private static bool CanEnchantEmber(CardModel card)
	{
		return ModelDb.Enchantment<TezcatarasEmber>().ToMutable().CanEnchant(card);
	}
}
