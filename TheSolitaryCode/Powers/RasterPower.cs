using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Powers;

// 光栅（Raster）的 Power（金卡 光栅 授予的常驻效果）：
// 每当拥有者打出一张附魔牌，获得 Amount 点格挡。
// 附魔牌判定参考元能吸附 EnergyAbsorptionPower.AfterCardPlayed。
[RegisterPower]
public sealed class RasterPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	// 每当拥有者打出一张附魔牌时触发，获得 Amount 点格挡。
	// 格挡来自 Power，使用 ValueProp.Unpowered（不享受力量/敏捷，参考余音 ReverbPower 的触发方式）。
	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner != base.Owner.Player || cardPlay.Card.Enchantment == null)
		{
			return;
		}

		Flash();
		await CreatureCmd.GainBlock(base.Owner, base.Amount, ValueProp.Unpowered, null);
	}
}
