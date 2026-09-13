using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
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
// 每当拥有者打出一张附魔牌时，生成一张随机术式+（升级版术式）并直接自动打出（不进手牌动画），目标随机敌方。
// 生成的术式始终为升级版（术式+），不受来源卡升级与否影响；全知形态的升级改为给卡牌自身加保留。
// 防递归（参考原版地狱使徒 HellraiserPower）：正在被本 Power 自动打出的术式会临时登记在 _autoPlayingArts 集合中，
// 若该术式在生成瞬间被附魔造物（万物通元）随机附魔，其 AfterCardPlayed 事件不会再次触发本 Power，避免无限连锁。
[RegisterPower]
public sealed class ArcaneReturnPower : ModPowerTemplate
{
	// 正在被本 Power 自动打出的术式（防递归守卫）。
	private readonly HashSet<CardModel> _autoPlayingArts = [];

	public override PowerType Type => PowerType.Buff;

	// 计数的层数型：叠多层时，每次附魔牌打出会生成并打出多张术式（参考能元妙术按 Amount 循环）。
	public override PowerStackType StackType => PowerStackType.Counter;

	// 自定义图标（取材 WatcherBeautified 观者 Power 图标，128x128，小图与大图共用）。
	public override PowerAssetProfile AssetProfile => new(
		IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
		BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png");

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

		// 归属校验已保证 Player 非空；战斗钩子内 CombatState 也必然存在。
		Player player = base.Owner.Player!;
		ICombatState combatState = cardPlay.Card.CombatState ?? base.Owner.CombatState!;
		for (int i = 0; i < base.Amount; i++)
		{
			// 生成一张随机术式+（始终为升级版）并直接快速自动打出（不进手牌、无进手牌动画；内部会叠加 ArtTrackerPower 记录生成数）。
			// 防递归（参考原版地狱使徒 HellraiserPower）：通过 beforeAutoPlay/afterAutoPlay 在自动打出前后
			// 登记/解除本 Power 正在打出的术式；若该术式在生成瞬间被附魔造物（万物通元）随机附魔，
			// 其 AfterCardPlayed 事件不会再次触发本 Power，避免无限连锁。
			await Arts.CreateRandomArtAndAutoPlay(
				player, combatState, player.RunState.Rng.CombatCardGeneration, choiceContext,
				upgraded: true,
				beforeAutoPlay: card => { _autoPlayingArts.Add(card); },
				afterAutoPlay: card => { _autoPlayingArts.Remove(card); });
		}
	}
}
