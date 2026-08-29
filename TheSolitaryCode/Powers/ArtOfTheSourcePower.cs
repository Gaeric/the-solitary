using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TheSolitary.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Powers;

// 能元妙术的 Power（参考无尽刀刃 InfiniteBladesPower）：
// 每回合开始（抽牌前的 BeforeHandDraw 钩子）向拥有者手中加入一张随机术式+（升级版术式）。
[RegisterPower]
public sealed class ArtOfTheSourcePower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	// 自定义图标（取材 WatcherBeautified 观者 Power 图标，128x128，小图与大图共用）。
	public override PowerAssetProfile AssetProfile => new(
		IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
		BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png");

	// 回合开始时为拥有者生成升级版术式；Amount 表示叠了层，每层生成一张。
	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
	{
		if (player != base.Owner.Player)
		{
			return;
		}

		Flash();
		for (int i = 0; i < base.Amount; i++)
		{
			await Arts.CreateRandomInHand(base.Owner.Player, combatState, player.RunState.Rng.CombatCardGeneration, choiceContext, upgraded: true);
		}
	}
}
