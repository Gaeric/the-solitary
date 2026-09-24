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


[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class EnchantedInsight : ModCardTemplate
{
	private const int BaseEnergyCost = 1;
	private const CardType CardKind = CardType.Skill;
	private const CardRarity CardRarityValue = CardRarity.Uncommon;
	private const TargetType CardTarget = TargetType.Self;
	private const bool ShowInCardLibrary = true;
	private const int DrawAmount = 2;

	public EnchantedInsight()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	public override bool GainsBlock => true;

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(DrawAmount),
	];

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		IEnumerable<CardModel> drawn = await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
		CardModel[] enchantedDrawn = drawn.Where(EnchantHelpers.HasValueEnchantment).ToArray();

                foreach (CardModel target in enchantedDrawn) {
                    EnchantHelpers.IncreaseEnchantmentValue(target, persistToDeckVersion: false);
                }
	}

	protected override void OnUpgrade()
	{
            DynamicVars.Cards.UpgradeValueBy(1);
	}
}
