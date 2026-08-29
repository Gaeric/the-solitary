using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Powers;

// 环回形态的 Power（参考计策 StratagemPower 的 AfterShuffle 钩子）：
// 每当拥有者的抽牌堆被打乱洗牌时累计次数，每累计 2 次洗牌获得 Amount 点能量后清零。
[RegisterPower]
public sealed class LoopFormPower : ModPowerTemplate
{
	// 洗牌阈值：固定每 2 次洗牌结算一次能量。
	private const int ShuffleThreshold = 2;

	// 距上次获得能量以来的洗牌次数（每 ShuffleThreshold 次结算一次后清零）。
	private int _shufflesSinceGain;

	private int ShufflesSinceGain
	{
		get
		{
			return _shufflesSinceGain;
		}
		set
		{
			AssertMutable();
			_shufflesSinceGain = value;
		}
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	// 自定义图标（取材 WatcherBeautified 观者 Power 图标，128x128，小图与大图共用）。
	public override PowerAssetProfile AssetProfile => new(
		IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
		BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png");

	// 悬停提示自动带上能量图标（显示 Amount 数值）。
	protected override bool IncludeEnergyHoverTip => true;

	// 抽牌堆被洗牌时触发（与 StratagemPower 相同的钩子：先校验洗牌者，再累计/结算）。
	public override async Task AfterShuffle(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != base.Owner.Player)
		{
			return;
		}

		ShufflesSinceGain++;
		if (ShufflesSinceGain < ShuffleThreshold)
		{
			return;
		}

		// 累计达到阈值次洗牌：清空计数并发放能量。
		ShufflesSinceGain = 0;
		Flash();
		await PlayerCmd.GainEnergy(base.Amount, base.Owner.Player);
	}
}
