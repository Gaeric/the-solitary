using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TheSolitary.Cards;
using TheSolitary.ArtEffects;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Powers;

// 能元妙术的 Power（参考无尽刀刃 InfiniteBladesPower）：
// 每回合开始（抽牌前）每层随机触发一次术式+。
[RegisterPower]
public sealed class ArtOfTheSourcePower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
		BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png");

	// Amount 表示层数，每层独立随机触发一次。
	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
	{
		if (player != base.Owner.Player)
		{
			return;
		}

		Flash();
		for (int i = 0; i < base.Amount; i++)
		{
			await ArtTrigger.TriggerRandom(player, combatState, player.RunState.Rng.CombatCardGeneration, choiceContext, upgraded: true);
		}
	}
}
