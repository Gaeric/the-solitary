using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using TheSolitary.Enchantments;

namespace TheSolitary.Cards;

// 拟合（character.org todo 白卡）：1 费技能。
// 获得 8 点格挡（升级后 11 点）；与手牌中一张牌交换附魔。
// 交换逻辑复用 EnchantHelpers.SwapEnchantmentWithHandCard（本卡 + 一张手牌交换）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Fitting : ModCardTemplate
{
	private const int BaseEnergyCost = 1;
	private const CardType CardKind = CardType.Skill;
	private const CardRarity CardRarityValue = CardRarity.Common;
	private const TargetType CardTarget = TargetType.Self;
	private const bool ShowInCardLibrary = true;

	public Fitting()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	public override bool GainsBlock => true;

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：格挡（绑定 {Block:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(8m, ValueProp.Move)
	];

	// 打出时：获得格挡，再与手牌中一张牌交换附魔。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

		// 与一张手牌交换附魔（附魔牌发光辅助规划；本卡与手牌都无附魔时跳过选择）。
		await EnchantHelpers.SwapEnchantmentWithHandCard(
			choiceContext,
			Owner,
			this,
			new CardSelectorPrefs(base.SelectionScreenPrompt, 1)
			{
				ShouldGlowGold = card => card.Enchantment != null
			},
			this);
	}

	// 升级：格挡 8 -> 11。
	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(3m);
	}
}
