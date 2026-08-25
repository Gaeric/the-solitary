using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Powers;

// 余音（character.org 蓝卡 #33，原名附魔守护）：每当你生成附魔时，获得 X 点格挡。
// 游戏没有任何"获得附魔后"的事件钩子，因此本 Power 不覆写钩子；
// 实际触发由 TheSolitaryCode/Patches/AfterEnchantPatch.cs 的 Harmony 补丁完成：
// 在 CardCmd.Enchant（所有附魔动作的唯一入口）成功后，检测到本 Power 即获得等量格挡。
[RegisterPower]
public sealed class ReverbPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;
}
