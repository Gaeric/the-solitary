using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Cards;
using TheSolitary.ArtEffects;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Powers;

// 余弦（Cosine）的 Power（蓝卡 余弦 的能力牌效果）：
// 每当拥有者触发一次术式时，获得 Amount 点格挡（包含回想的额外结算）。
[RegisterPower]
public sealed class CosinePower : ModPowerTemplate, IArtTriggerListener
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
		BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png");

	// 专用术式钩子：只处理拥有者的触发。
	public async Task AfterArtTriggered(PlayerChoiceContext choiceContext, ArtTriggerResult trigger)
	{
		if (trigger.Owner.Creature != base.Owner)
		{
			return;
		}

		Flash();
		await CreatureCmd.GainBlock(base.Owner, base.Amount, ValueProp.Unpowered, null);
	}
}
