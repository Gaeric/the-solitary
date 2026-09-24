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
public sealed class RingOfSoulReturn : ModCardTemplate
{
    private const int BaseEnergyCost = 1;
    private const CardType CardKind = CardType.Skill;
    private const CardRarity CardRarityValue = CardRarity.Uncommon;
    private const TargetType CardTarget = TargetType.Self;
    private const bool ShowInCardLibrary = true;

    public RingOfSoulReturn()
    : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    public override bool GainsBlock => true;

    public override CardAssetProfile AssetProfile => new(
	PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(7m, ValueProp.Move)
	];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		HoverTipFactory.FromEnchantment<SoulsPower>();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
	await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

	await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

	if (!PileType.Draw.GetPile(Owner).Cards.Any(CanEnchantSoulsPower))
	{
	    return;
	}

	CardModel? picked = (await CardSelectCmd.FromCombatPile(
	    prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 1),
	    context: choiceContext,
	    pile: PileType.Draw.GetPile(Owner),
	    player: Owner,
	    filter: CanEnchantSoulsPower)).FirstOrDefault();

	if (picked != null)
	{
	    CardCmd.Enchant<SoulsPower>(picked, 1m);
	}
    }

    protected override void OnUpgrade()
    {
	AddKeyword(CardKeyword.Innate);
    }

    private static bool CanEnchantSoulsPower(CardModel card)
    {
	return ModelDb.Enchantment<SoulsPower>().ToMutable().CanEnchant(card);
    }
}
