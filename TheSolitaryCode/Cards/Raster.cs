using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheSolitary.Characters;
using TheSolitary.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 光栅（character.org todo 金卡）：1 费能力牌。
// 每当你打出一张附魔牌，获得 2 点格挡。升级后固有。
// 能力牌 + 持续 Power 的模式参考余弦 Cosine / 元能吸附 EnergyAbsorption（RasterPower 做实际触发）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Raster : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（能力）。
	private const CardType CardKind = CardType.Power;
	// 卡牌稀有度（金卡 = Rare）。
	private const CardRarity CardRarityValue = CardRarity.Rare;
	// 目标类型（自身）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public Raster()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Raster.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：每次打出一张附魔牌获得的格挡（绑定 {RasterPower:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<RasterPower>(2m)
	];

	// 打出时：播放能力施放动画，授予常驻 RasterPower（层数 = 每次获得的格挡）。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
		await PowerCmd.Apply<RasterPower>(choiceContext, Owner.Creature,
			DynamicVars["RasterPower"].BaseValue, Owner.Creature, this);
	}

	// 升级：获得固有。
	protected override void OnUpgrade()
	{
		AddKeyword(CardKeyword.Innate);
	}
}
