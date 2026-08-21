using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Powers;

// 环回形态的 Power（参考计策 StratagemPower 的 AfterShuffle 钩子）：
// 每当拥有者的抽牌堆被打乱洗牌时，获得 Amount 点能量（每层 1 点）。
[RegisterPower]
public sealed class LoopFormPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	// 悬停提示自动带上能量图标（显示 Amount 数值）。
	protected override bool IncludeEnergyHoverTip => true;

	// 抽牌堆被洗牌时触发（与 StratagemPower 相同的钩子：先校验洗牌者，再 Flash + 生效）。
	public override async Task AfterShuffle(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != base.Owner.Player)
		{
			return;
		}

		Flash();
		await PlayerCmd.GainEnergy(base.Amount, base.Owner.Player);
	}
}
