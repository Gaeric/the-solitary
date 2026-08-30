using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Powers;

// 斗转星移的 Power（参考原版神气制胜 PanachePower / 环绕轨道 OrbitPower 的进度提示方案）：
// 每当拥有者的抽牌堆被打乱洗牌时累计次数，每累计 2 次洗牌获得 Amount 点能量后清零。
// 图标上的数字 = 距下次触发还差几次洗牌；悬停提示通过 {ShufflesLeft} 占位符实时显示剩余次数。
[RegisterPower]
public sealed class LoopFormPower : ModPowerTemplate
{
	// 洗牌阈值：固定每 2 次洗牌结算一次能量。
	private const int ShuffleThreshold = 2;

	// 剩余洗牌次数的 DynamicVar 键名（与 powers.json smartDescription 中的 {ShufflesLeft} 占位符对应）。
	private const string ShufflesLeftKey = "ShufflesLeft";

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	// 自定义图标（取材 WatcherBeautified 观者 Power 图标，128x128，小图与大图共用）。
	public override PowerAssetProfile AssetProfile => new(
		IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
		BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png");

	// 悬停提示自动带上能量图标（显示 Amount 数值）。
	protected override bool IncludeEnergyHoverTip => true;

	// 图标上的数字 = 距下次触发还差几次洗牌（参考神气制胜 PanachePower 的 DisplayAmount 覆写）。
	public override int DisplayAmount => base.DynamicVars[ShufflesLeftKey].IntValue;

	// 进度变量：每洗牌 1 次减 1，归零触发后重置为阈值；自动绑定 smartDescription 的 {ShufflesLeft} 占位符。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar(ShufflesLeftKey, ShuffleThreshold)
	];

	// 抽牌堆被洗牌时触发（与 StratagemPower 相同的钩子：先校验洗牌者，再累计/结算）。
	public override async Task AfterShuffle(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != base.Owner.Player)
		{
			return;
		}

		base.DynamicVars[ShufflesLeftKey].BaseValue--;
		InvokeDisplayAmountChanged();
		if (base.DynamicVars[ShufflesLeftKey].IntValue > 0)
		{
			return;
		}

		// 累计达到阈值次洗牌：重置进度并发放能量。
		base.DynamicVars[ShufflesLeftKey].BaseValue = ShuffleThreshold;
		InvokeDisplayAmountChanged();
		Flash();
		await PlayerCmd.GainEnergy(base.Amount, base.Owner.Player);
	}
}
