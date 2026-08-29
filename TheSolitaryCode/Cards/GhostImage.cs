using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 重影（character.org todo 金卡）：X 费能力牌。
// 获得 3 层覆甲 X 次（升级后每次 4 层），即总共 3x / 4x 层覆甲。
// X 费机制参考散射 Scatter / 原版穿刺 Skewer（HasEnergyCostX + ResolveEnergyXValue 消耗所有能量）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class GhostImage : ModCardTemplate
{
	// X 费：基础耗能显示为 0，实际消耗所有能量。
	private const int BaseEnergyCost = 0;
	// 卡牌类型（能力）。
	private const CardType CardKind = CardType.Power;
	// 卡牌稀有度（金卡 = Rare）。
	private const CardRarity CardRarityValue = CardRarity.Rare;
	// 目标类型（自身）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	// X 费：消耗所有能量，每 1 点能量生效一次。
	protected override bool HasEnergyCostX => true;

	public GhostImage()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/GhostImage.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 悬停提示：展示覆甲的机制说明。
	protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<PlatingPower>()];

	// 基础数值：每次获得的覆甲层数（绑定 {PlatingPower:diff()} 占位符，与 PowerVar 键名一致）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<PlatingPower>(3m)
	];

	// 打出时：按消耗的能量 X 逐次获得覆甲（每次独立结算，覆甲层数累加）。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
		int x = ResolveEnergyXValue();
		for (int i = 0; i < x; i++)
		{
			await PowerCmd.Apply<PlatingPower>(choiceContext, Owner.Creature,
				DynamicVars["PlatingPower"].BaseValue, Owner.Creature, this);
		}
	}

	// 升级：每次覆甲 3 -> 4 层。
	protected override void OnUpgrade()
	{
		DynamicVars["PlatingPower"].UpgradeValueBy(1m);
	}
}
