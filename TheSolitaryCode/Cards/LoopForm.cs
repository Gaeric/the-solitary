using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheSolitary.Characters;
using TheSolitary.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 环回形态（character.org 金卡 #5）：1 费能力牌。
// 每当你的抽牌堆打乱洗牌时，获得 1 点能量（升级后 0 费）。
// 实现参考计策（Stratagem）：能力牌打出后给自己施加 Power，
// 通过 Power 的 AfterShuffle 钩子响应“抽牌堆被打乱洗牌”事件（CardPileCmd 洗牌后触发）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class LoopForm : ModCardTemplate
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

	public LoopForm()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/LoopForm.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：每次洗牌获得的能量（绑定 {Energy:energyIcons()} 占位符；与 Power 每层 Amount 保持一致）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new EnergyVar(1)
	];

	// 打出时：给自己施加环回形态 Power（层数 = 每次洗牌获得的能量）。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PowerCmd.Apply<LoopFormPower>(choiceContext, Owner.Creature, DynamicVars.Energy.BaseValue, Owner.Creature, this);
	}

	// 升级：费用 1 -> 0（参考计策 Stratagem 的升级方式）。
	protected override void OnUpgrade()
	{
		base.EnergyCost.UpgradeBy(-1);
	}
}
