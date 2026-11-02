using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TheSolitary.ArtEffects;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Powers;

// 全知形态：每打出一张附魔牌，每层随机触发一次术式+。
// 独立术式不会生成/打出卡牌，因此不需要旧的自动打牌递归守卫。
[RegisterPower]
public sealed class ArcaneReturnPower : ModPowerTemplate
{

	public override PowerType Type => PowerType.Buff;

	// 计数的层数型：每层分别随机触发一次。
	public override PowerStackType StackType => PowerStackType.Counter;

	// 自定义图标（取材 WatcherBeautified 观者 Power 图标，128x128，小图与大图共用）。
	public override PowerAssetProfile AssetProfile => new(
		IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
		BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png");

	// 只响应真正打出的附魔牌，术式触发不会进入此钩子。
	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		// 归属校验：只响应拥有者打出的牌（参考原版狂怒 RagePower）。
		if (cardPlay.Card.Owner != base.Owner.Player)
		{
			return;
		}
		// 只响应附魔牌。
		if (cardPlay.Card.Enchantment == null)
		{
			return;
		}

		Flash();

		// 归属校验已保证 Player 非空；战斗钩子内 CombatState 也必然存在。
		Player player = base.Owner.Player!;
		ICombatState combatState = cardPlay.Card.CombatState ?? base.Owner.CombatState!;
		for (int i = 0; i < base.Amount; i++)
		{
			// 每次独立随机选种类、选敌，保留术式+。
			await ArtTrigger.TriggerRandom(
				player, combatState, player.RunState.Rng.CombatCardGeneration, choiceContext,
				upgraded: true);
		}
	}
}
