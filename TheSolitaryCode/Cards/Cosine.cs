using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using TheSolitary.ArtEffects;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheSolitary.Characters;
using TheSolitary.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 余弦（character.org todo 蓝卡）：1 费能力牌，升级后 0 费。
// 每触发一次术式，获得 2 点格挡；由 CosinePower 的专用术式钩子实现。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Cosine : ModCardTemplate
{
	protected override IEnumerable<IHoverTip> AdditionalHoverTips => ArtHoverTips.Create();

	private const int BaseEnergyCost = 1;
	private const CardType CardKind = CardType.Power;
	private const CardRarity CardRarityValue = CardRarity.Uncommon;
	private const TargetType CardTarget = TargetType.Self;
	private const bool ShowInCardLibrary = true;

	public Cosine()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：每次生成术式获得的格挡（绑定 {CosinePower:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<CosinePower>(2m)
	];

	// 打出时：授予常驻 CosinePower（层数 = 每次生成的术式获得的格挡）。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
		await PowerCmd.Apply<CosinePower>(choiceContext, Owner.Creature,
			DynamicVars["CosinePower"].BaseValue, Owner.Creature, this);
	}

	// 升级：费用 1 -> 0。
	protected override void OnUpgrade()
	{
		base.EnergyCost.UpgradeBy(-1);
	}
}
