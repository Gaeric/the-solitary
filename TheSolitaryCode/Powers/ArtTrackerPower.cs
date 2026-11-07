using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Powers;

// 隐藏计数器 Power：Amount 记录本场战斗中该玩家实际触发的术式次数（含回想）。
// 随战斗结束自动移除（战斗结束时 Power 会被清除），天然按战斗重置。
// 供奇点爆破读取；由 ArtTrigger 在每次实际结算前累计。
[RegisterPower]
public sealed class ArtTrackerPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
		BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png");

	// 不显示在状态栏/战斗日志中。
	protected override bool IsVisibleInternal => false;
}
