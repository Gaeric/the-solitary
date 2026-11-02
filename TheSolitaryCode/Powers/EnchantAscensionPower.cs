using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using TheSolitary.Rewards;

namespace TheSolitary.Powers;

// 参考禁忌魔典：仅由打出卡牌施加，每层在战后奖励列表增加一次选牌机会。
[RegisterPower]
public sealed class EnchantAscensionPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;
	public override PowerStackType StackType => PowerStackType.Counter;
	public override PowerAssetProfile AssetProfile => new(
		IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
		BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png");

	public override Task AfterCombatEnd(CombatRoom room)
	{
		for (int i = 0; i < Amount; i++)
		{
			room.AddExtraReward(Owner.Player!, new RandomEnchantmentReward(Owner.Player!));
		}
		return Task.CompletedTask;
	}
}