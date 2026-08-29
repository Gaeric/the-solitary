using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TheSolitary.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Powers;

// 术法归元的 Power（参考原版模仿学习 ImitationLearningPower 的 AfterCardPlayed 钩子 + CardCmd.AutoPlay 自动打出）：
// 每当拥有者打出一张附魔牌时，生成一张随机术式到手中，并以随机敌方为目标自动打出。
// 升级后（来源卡为升级版术法归元）生成的术式变为术式+（升级版）：
// 在 AfterApplied 捕获来源卡，触发时按 IsUpgraded 判定。
// 防递归（参考原版地狱使徒 HellraiserPower）：正在被本 Power 自动打出的术式会临时登记在 _autoPlayingArts 集合中，
// 若该术式在生成瞬间被附魔造物（万物通元）随机附魔，其 AfterCardPlayed 事件不会再次触发本 Power，避免无限连锁。
[RegisterPower]
public sealed class ArcaneReturnPower : ModPowerTemplate
{
	// 正在被本 Power 自动打出的术式（防递归守卫）。
	private readonly HashSet<CardModel> _autoPlayingArts = [];

	// 来源卡（术法归元）：升级后每次触发生成术式+。
	private ArcaneReturn? _sourceCard;

	public override PowerType Type => PowerType.Buff;

	// 计数的层数型：叠多层时，每次附魔牌打出会生成并打出多张术式（参考能元妙术按 Amount 循环）。
	public override PowerStackType StackType => PowerStackType.Counter;

	// 自定义图标（取材 WatcherBeautified 观者 Power 图标，128x128，小图与大图共用）。
	public override PowerAssetProfile AssetProfile => new(
		IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
		BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png");

	// 施加时捕获来源卡，用于判断生成普通术式还是术式+。
	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		_sourceCard = cardSource as ArcaneReturn;
		return Task.CompletedTask;
	}

	// 每当拥有者打出一张附魔牌：生成随机术式，并以随机敌方目标自动打出。
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
		// 防递归：跳过本 Power 自己自动打出的术式（可能已被万物通元附魔）。
		if (_autoPlayingArts.Contains(cardPlay.Card))
		{
			return;
		}

		Flash();

		// 来源卡升级时生成术式+。
		bool upgraded = _sourceCard?.IsUpgraded ?? false;
		// 归属校验已保证 Player 非空；战斗钩子内 CombatState 也必然存在。
		Player player = base.Owner.Player!;
		ICombatState combatState = cardPlay.Card.CombatState ?? base.Owner.CombatState!;
		for (int i = 0; i < base.Amount; i++)
		{
			// 生成一张随机术式（或术式+）到手牌中（内部会叠加 ArtTrackerPower 记录生成数）。
			CardModel art = await Arts.CreateRandomInHand(player, combatState, player.RunState.Rng.CombatCardGeneration, choiceContext, upgraded: upgraded);
			// 自动打出：target 传 null 时 CardCmd.AutoPlay 会从可命中敌人中随机选一个目标。
			_autoPlayingArts.Add(art);
			await CardCmd.AutoPlay(choiceContext, art, null);
			_autoPlayingArts.Remove(art);
		}
	}
}
