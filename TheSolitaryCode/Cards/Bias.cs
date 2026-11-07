using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 偏置（character.org todo 白卡）：1 费技能。
// 获得 5 点格挡（升级后 8 点）；如果这张卡自身带有附魔，额外再获得一次格挡。
// “有附魔”指卡牌自身带有附魔（例如打出前被附魔了伶俐/墨影等）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Bias : ModCardTemplate
{
	private const int BaseEnergyCost = 1;
	private const CardType CardKind = CardType.Skill;
	private const CardRarity CardRarityValue = CardRarity.Common;
	private const TargetType CardTarget = TargetType.Self;
	private const bool ShowInCardLibrary = true;

	public Bias()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	public override bool GainsBlock => true;

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：每次获得的格挡（绑定 {Block:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(5m, ValueProp.Move)
	];

	// 打出时：获得格挡；如果这张卡自身带有附魔，额外再获得一次格挡（每次独立结算，敏捷每次生效）。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

		// “有附魔”指这张卡自身带有附魔（cardPlay.Card 即当前打出的卡牌实例）。
		if (cardPlay.Card.Enchantment != null)
		{
			await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
		}
	}

	// 升级：格挡 5 -> 8。
	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(3m);
	}
}
