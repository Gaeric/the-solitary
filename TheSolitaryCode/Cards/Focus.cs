using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheSolitary.Characters;
using TheSolitary.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 聚焦（新卡，参考原版 Tracking 跟踪）：2 费能力牌（金卡）。
// 敌人每有一种负面效果，受到的伤害额外增加 10%（升级后 1 费，效果不变）。
// 与原版 Tracking 的区别：Tracking 只对"攻击伤害"生效（TrackingPower 里用
// props.IsPoweredAttack() 过滤、且目标必须带虚弱）；本卡不做攻击限制——
// 对全部伤害来源生效（攻击/卡牌/能力/遗物等），且按负面效果种类数按比例放大。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Focus : ModCardTemplate
{
	private const int BaseEnergyCost = 2;
	private const CardType CardKind = CardType.Power;
	private const CardRarity CardRarityValue = CardRarity.Rare;
	private const TargetType CardTarget = TargetType.Self;
	private const bool ShowInCardLibrary = true;

	public Focus()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：每种负面效果增加的伤害百分比（绑定 {FocusPower:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<FocusPower>(10m)
	];

	// 打出时：播放能力施放动画，授予常驻 FocusPower（层数 = 每种负面效果的伤害加成百分比）。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
		await PowerCmd.Apply<FocusPower>(choiceContext, Owner.Creature,
			DynamicVars["FocusPower"].BaseValue, Owner.Creature, this);
	}

	// 升级：费用 2 -> 1（效果不变，参考原版 Tracking 的升级方式）。
	protected override void OnUpgrade()
	{
		base.EnergyCost.UpgradeBy(-1);
	}
}
