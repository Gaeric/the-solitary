using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheSolitary.Characters;
using TheSolitary.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 播种（character.org 蓝卡 #4）：1 费能力牌。
// 当你前 2 次洗牌时，选择一张手牌附魔播种（Sown：每场战斗第一次打出时获得 1 点能量）。
// 升级后 0 费。实现参考计策（Stratagem）：能力牌打出后给自己施加 Power，
// 通过 Power 的 AfterShuffle 钩子响应“抽牌堆被打乱洗牌”事件。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Sowing : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（能力）。
	private const CardType CardKind = CardType.Power;
	// 卡牌稀有度（蓝卡 = Uncommon）。
	private const CardRarity CardRarityValue = CardRarity.Uncommon;
	// 目标类型（自身）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;
	// 洗牌触发次数的 DynamicVar 键名（绑定 {ShuffleCount:diff()} 占位符）。
	private const string ShuffleCountKey = "ShuffleCount";
	// 前几次洗牌触发（与 Power 的初始层数保持一致）。
	private const int ShuffleCount = 2;

	public Sowing()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Sowing.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：前 2 次洗牌触发（与 Power 初始层数保持一致）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar(ShuffleCountKey, ShuffleCount)
	];

	// 打出时：播放 Power 施放动画，并给自己叠一层 SowingPower（层数 = 前几次洗牌触发）。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
		await PowerCmd.Apply<SowingPower>(choiceContext, Owner.Creature, DynamicVars[ShuffleCountKey].BaseValue, Owner.Creature, this);
	}

	// 升级：费用 1 -> 0（参考计策 Stratagem 的升级方式）。
	protected override void OnUpgrade()
	{
		base.EnergyCost.UpgradeBy(-1);
	}
}
