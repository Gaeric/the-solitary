using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 散射（character.org 蓝卡，英文名 Scatter）：X 费技能。
// 获得 7 点格挡 X 次（升级后 9 点）；每次获得格挡单独结算，敏捷每次生效（参考原版 强化躯体 Reinforced Body 的逐次获得）。
// X 费机制参考原版 穿刺 Skewer：HasEnergyCostX + ResolveEnergyXValue（消耗所有能量，每 1 点能量生效一次）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Scatter : ModCardTemplate
{
	// X 费：基础耗能显示为 0，实际消耗所有能量。
	private const int BaseEnergyCost = 0;
	private const CardType CardKind = CardType.Skill;
	private const CardRarity CardRarityValue = CardRarity.Uncommon;
	private const TargetType CardTarget = TargetType.Self;
	private const bool ShowInCardLibrary = true;

	// X 费：消耗所有能量，每 1 点能量生效一次。
	protected override bool HasEnergyCostX => true;

	public Scatter()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	public override bool GainsBlock => true;

	// 基础数值：每次获得 7 点格挡（升级后 9），绑定 {Block:diff()} 占位符。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(7m, ValueProp.Move)
	];

	// 打出时：获得 7 点格挡 X 次（每次独立结算，敏捷每次生效）。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		int x = ResolveEnergyXValue();
		for (int i = 0; i < x; i++)
		{
			await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
		}
	}

	// 升级：每次格挡 7 -> 9。
	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(2m);
	}
}
