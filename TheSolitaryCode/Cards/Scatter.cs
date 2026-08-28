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
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（蓝卡 = Uncommon，参考原版 X 费卡 Skewer / 强化躯体）。
	private const CardRarity CardRarityValue = CardRarity.Uncommon;
	// 目标类型（自身）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	// X 费：消耗所有能量，每 1 点能量生效一次。
	protected override bool HasEnergyCostX => true;

	public Scatter()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Scatter.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 获得格挡，按格挡牌参与力量/敏捷等计算。
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
