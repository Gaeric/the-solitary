using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheSolitary.Characters;
using TheSolitary.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 辐照（新卡）：2 费能力牌（蓝卡）。
// 每当你打出一张附魔牌，对所有角色造成 2 点伤害（升级后 3 点）。
// "所有角色" = 战斗中所有可命中角色（我方 + 敌方，含多人模式全部玩家角色），
// 用 CombatState.Creatures 枚举全部角色、IsHittable 过滤存活可命中者。
// 能力牌 + 持续 Power 的模式参考光栅 Raster（RasterPower 做实际触发）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Irradiation : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 2;
	// 卡牌类型（能力）。
	private const CardType CardKind = CardType.Power;
	// 卡牌稀有度（蓝卡 = Uncommon）。
	private const CardRarity CardRarityValue = CardRarity.Uncommon;
	// 目标类型（自身）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public Irradiation()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Irradiation.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：每次打出一张附魔牌对所有角色造成的伤害（绑定 {IrradiationPower:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<IrradiationPower>(2m)
	];

	// 打出时：播放能力施放动画，授予常驻 IrradiationPower（层数 = 每次造成的伤害）。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
		await PowerCmd.Apply<IrradiationPower>(choiceContext, Owner.Creature,
			DynamicVars["IrradiationPower"].BaseValue, Owner.Creature, this);
	}

	// 升级：伤害 2 -> 3。
	protected override void OnUpgrade()
	{
		DynamicVars["IrradiationPower"].UpgradeValueBy(1m);
	}
}
