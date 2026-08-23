using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Powers;

// 附魔共鸣（character.org 蓝卡 #27）：每当你获得附魔时，获得 X 点活力（活力=下次攻击伤害+X）。
// 游戏没有任何"获得附魔后"的事件钩子，因此本 Power 不覆写钩子；
// 实际触发由 TheSolitaryCode/Patches/EnchantResonancePatch.cs 的 Harmony 补丁完成：
// 在 CardCmd.Enchant（所有附魔动作的唯一入口）成功后，检测到本 Power 即施加等量活力。
[RegisterPower]
public sealed class EnchantResonancePower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;
}
