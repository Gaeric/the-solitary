using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;
using TheSolitary.Powers;

namespace TheSolitary.Cards;

// 术式：TheSolitary 的衍生牌集合（类似小刀），由其它卡牌/遗物生成后加入手牌。
// 共五种：术式-凋零（缓慢）、术式-枯萎（虚弱）、术式-破袭（易伤）、术式-浸毒（中毒）、术式-灾引（灾厄）。
public static class Arts
{
	// 五种术式的原型（canonical）实例，从 ModelDb 中获取。
	private static readonly CardModel[] All =
	[
		ModelDb.Card<ArtOfDecay>(),
		ModelDb.Card<ArtOfWilt>(),
		ModelDb.Card<ArtOfBreach>(),
		ModelDb.Card<ArtOfVenom>(),
		ModelDb.Card<ArtOfDoom>()
	];

	/// <summary>
	/// 生成一张随机的术式并加入手中（等价于 Shiv.CreateInHand 的模式）。
	/// 术式-凋零只有在"至少一个可被攻击的敌人还没有缓慢效果"时才可能被选中，
	/// 避免在全员都已有缓慢时生成一张几乎无效的术式-凋零。
	/// 同时会给拥有者叠加一层隐藏的 ArtTrackerPower，记录本场战斗生成的术式总数。
	/// </summary>
	/// <param name="owner">持有者玩家。</param>
	/// <param name="combatState">当前战斗状态。</param>
	/// <param name="rng">用于随机挑选术式的 RNG（建议传入 RunState.Rng.CombatCardGeneration）。</param>
	/// <param name="choiceContext">用于施加计数器 Power 的选择上下文。</param>
	/// <param name="creator">生成来源（用于统计/来源记录，缺省为 owner）。</param>
	/// <param name="upgraded">是否生成升级版术式（术式+），参考 Largesse / ManifestAuthority 的升级判定模式。</param>
	public static async Task<CardModel> CreateRandomInHand(Player owner, ICombatState combatState, Rng rng, PlayerChoiceContext choiceContext, Player? creator = null, bool upgraded = false)
	{
		// 只要存在一个可命中敌人没有缓慢，就保留术式-凋零作为候选；否则排除它。
		bool anyHittableEnemyWithoutSlow = combatState.HittableEnemies.Any(e => !e.HasPower<SlowPower>());
		IEnumerable<CardModel> candidates = anyHittableEnemyWithoutSlow
			? All
			: All.Where(c => c != ModelDb.Card<ArtOfDecay>());

		CardModel canonical = rng.NextItem(candidates)!;
		CardModel card = combatState.CreateCard(canonical, owner);
		// 升级后的术式：在加入战斗前对生成的实例调用 CardCmd.Upgrade（与 Largesse 生成升级无色牌同款）。
		if (upgraded)
		{
			CardCmd.Upgrade(card);
		}
		await CardPileCmd.AddGeneratedCardsToCombat([card], PileType.Hand, creator ?? owner);

		// 记录本场战斗为该玩家生成的术式数量 +1（供“造成生成数伤害”的卡牌使用）。
		await PowerCmd.Apply<ArtTrackerPower>(choiceContext, owner.Creature, 1m, owner.Creature, null);

		return card;
	}

	/// <summary>
	/// 生成一张随机术式到手牌并立即快速自动打出（路径追踪 / 全知形态术法归元共用的快节奏流程）。
	/// 保留「进手牌」动画让玩家看清随机生成了什么术式，随后把卡牌节点快速移到打出区（0.1s 短 tween），
	/// 自动打出时跳过牌堆移动/等待动画（skipCardPileVisuals），只保留术式自身的攻击/命中动画，
	/// 结束后清理打出区残留的卡牌节点（skipCardPileVisuals 不会清理节点，否则会卡在 UI 中）。
	/// 内部先调用 <see cref="CreateRandomInHand"/>，因此随机/升级/生成钩子/ArtTrackerPower 计数逻辑完全一致。
	/// </summary>
	/// <param name="beforeAutoPlay">自动打出开始前回调（可用于防递归登记正在打出的术式）。</param>
	/// <param name="afterAutoPlay">自动打出结束后回调（可用于解除防递归登记）。</param>
	public static async Task<CardModel> CreateRandomInHandAndFastPlay(
		Player owner,
		ICombatState combatState,
		Rng rng,
		PlayerChoiceContext choiceContext,
		Player? creator = null,
		bool upgraded = false,
		Action<CardModel>? beforeAutoPlay = null,
		Action<CardModel>? afterAutoPlay = null)
	{
		// 先生成到手牌（保留进手牌动画，玩家能看到随机生成了什么术式）。
		CardModel card = await CreateRandomInHand(owner, combatState, rng, choiceContext, creator, upgraded);

		// 把卡牌节点从手牌快速移到打出区（0.1s 短 tween，替代原版 0.25s 的长 tween），
		// 结算时术式在打出区可见。若节点不在手牌（极端情况），返回 false 并退回完整动画路径，
		// 保证 UI 不会被卡牌残留。
		bool moved = await TryMoveArtNodeToPlayArea(card);

		beforeAutoPlay?.Invoke(card);

		// 自动打出：跳过牌堆移动/等待动画（省去手牌→打出区 tween、0.25~0.35s 固定等待与打出区→弃牌 tween），
		// 只保留术式自身的攻击/命中动画，避免连续多张术式时动画逐张拖沓。
		await CardCmd.AutoPlay(choiceContext, card, null, skipCardPileVisuals: moved);

		afterAutoPlay?.Invoke(card);

		// skipCardPileVisuals 不会清理卡牌节点：手动移除打出区残留的节点，否则会卡在 UI 中。
		if (moved)
		{
			RemovePlayAreaNode(card);
		}

		return card;
	}

	// 把术式的卡牌节点从手牌快速移到打出区。
	// 返回 false 表示未找到手牌节点（此时调用方应退回完整动画路径）。
	private static async Task<bool> TryMoveArtNodeToPlayArea(CardModel card)
	{
		NCombatRoom? combatRoom = NCombatRoom.Instance;
		if (combatRoom == null)
		{
			return false;
		}
		NPlayerHand? hand = combatRoom.Ui.Hand;
		if (hand == null)
		{
			return false;
		}
		NCardHolder? holder = hand.GetCardHolder(card);
		NCard? node = holder?.CardNode;
		if (holder == null || node == null)
		{
			return false;
		}

		// 用标准手牌移除流程摘除 holder（会取消事件订阅并解除节点绑定），再把节点放进打出区展示。
		// 顺序不能反：先 RemoveCardHolder 会让 holder.Clear() 把节点从树上摘下来（但不会释放节点），
		// 随后 AddToPlayContainer 才会把该节点重新挂到打出区。
		hand.RemoveCardHolder(holder);
		combatRoom.Ui.AddToPlayContainer(node);
		node.UpdateVisuals(PileType.Play, CardPreviewMode.Normal);
		node.Scale = Vector2.One * 0.8f;

		// 0.1s 快速飞到打出区目标位置（替代原版 0.25s 的 AppendPlayPileLerpTween）。
		Vector2 targetPosition = PileType.Play.GetTargetPosition(node);
		Tween tween = node.CreateTween();
		tween.TweenProperty(node, "position", targetPosition, 0.1f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
		await tween.AwaitFinished(combatRoom);
		return true;
	}

	// 移除自动打出（skipCardPileVisuals）后残留在打出区的卡牌节点。
	private static void RemovePlayAreaNode(CardModel card)
	{
		NCard? node = NCombatRoom.Instance?.Ui.GetCardFromPlayContainer(card);
		node?.QueueFreeSafely();
	}

	/// <summary>
	/// 判断一张牌是否为术式（术式-凋零/术式-枯萎/术式-破袭/术式-浸毒/术式-灾引）。
	/// </summary>
	public static bool IsArt(CardModel card)
	{
		return card is ArtOfDecay or ArtOfWilt or ArtOfBreach or ArtOfVenom or ArtOfDoom;
	}
}
