using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Powers;

// 隐藏计数器 Power：Amount 记录本场战斗中为该玩家生成的术式数量。
// 随战斗结束自动移除（战斗结束时 Power 会被清除），天然按战斗重置。
// 供“造成本场战斗生成的术式数量伤害”的卡牌读取（GetPowerAmount<ArtTrackerPower>）。
[RegisterPower]
public sealed class ArtTrackerPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	// 不显示在状态栏/战斗日志中。
	protected override bool IsVisibleInternal => false;
}
