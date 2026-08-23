using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

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
	/// 交换逻辑已抽离为 EnchantHelpers.SwapEnchantmentsBetweenTwoHandCards，与蓝卡 #26 能力牌共用。
	/// </summary>
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		// 设计文档（character.org 蓝卡 #3）：先获得 5 点格挡，再交换两张手牌附魔。
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

		// 选择恰好两张手牌；附魔牌发光辅助规划。选择不足两张则无事发生。
		await EnchantHelpers.SwapEnchantmentsBetweenTwoHandCards(
			choiceContext,
			Owner,
			new CardSelectorPrefs(base.SelectionScreenPrompt, 2)
			{
				ShouldGlowGold = card => card.Enchantment != null
			},
			this);
	}

	// Upgraded: the card becomes free (Block and the swap effect keep their base values).
	protected override void OnUpgrade()
	{
		base.EnergyCost.UpgradeBy(-1);
	}
}

