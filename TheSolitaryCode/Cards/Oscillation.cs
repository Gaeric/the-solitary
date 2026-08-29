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

// 振荡（新卡，参考原版巨像 Colossus）：1 费技能牌（蓝卡）。
// 获得 8 点格挡（升级后 11）；在本回合中，敌人每有一种负面效果，对你造成的伤害降低 10%。
// 实现参考 Colossus：格挡 + 施加一层临时 Power；
// Power 覆写 ModifyDamageMultiplicative 减伤，并在敌方回合结束时 PowerCmd.Decrement 移除
// （Amount=1 → 只持续本回合）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Oscillation : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（蓝卡 = Uncommon）。
	private const CardRarity CardRarityValue = CardRarity.Uncommon;
	// 目标类型（自身）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public Oscillation()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 获得格挡，按格挡牌参与力量/敏捷等计算。
	public override bool GainsBlock => true;

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Oscillation.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：格挡 8（升级后 11）+ 每种负面效果的减伤百分比（绑定 {OscillationReduction:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(8m, ValueProp.Move),
		new DynamicVar("OscillationReduction", 10m)
	];

	// 打出时：先获得格挡，再施加 1 层临时 OscillationPower。
	// Amount=1 表示"剩余敌方回合数"（敌方回合结束 Decrement，减到 0 自动移除），
	// 减伤百分比由 Power 内部常量计算，与 OscillationReduction 占位符保持一致。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
		await PowerCmd.Apply<OscillationPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
	}

	// 升级：格挡 8 -> 11（减伤效果不变）。
	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(3m);
	}
}
