using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Characters;
using TheSolitary.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 动能回收（character.org 蓝卡 #15）：1 费技能，消耗。
// 获得 8 点格挡（升级后 11），并让你下一张打出的攻击牌获得动量附魔。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class KineticRecovery : ModCardTemplate
{
	private const int BaseEnergyCost = 1;
	private const CardType CardKind = CardType.Skill;
	private const CardRarity CardRarityValue = CardRarity.Uncommon;
	private const TargetType CardTarget = TargetType.Self;
	private const bool ShowInCardLibrary = true;
	// 剩余可附魔的攻击牌张数（固定 1 张，升级不增加）。
	private const int AttacksToEnchant = 1;

	public KineticRecovery()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	public override bool GainsBlock => true;

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

	// 基础数值：格挡 8（升级后 11），绑定 {Block:diff()} 占位符。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(8m, ValueProp.Move)
	];

	// 打出时：获得格挡，再授予「下一张攻击牌获得动量」的 Power（层数固定 1）。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
		await PowerCmd.Apply<KineticRecoveryPower>(choiceContext, Owner.Creature, AttacksToEnchant, Owner.Creature, this);
	}

	// 升级：格挡 8 -> 11（可附魔攻击牌张数保持 1）。
	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(3m);
	}
}
