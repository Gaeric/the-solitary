using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
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
		CardModel card = CreateRandomArtCard(owner, combatState, rng, upgraded);
		await CardPileCmd.AddGeneratedCardsToCombat([card], PileType.Hand, creator ?? owner);

		// 记录本场战斗为该玩家生成的术式数量 +1（供“造成生成数伤害”的卡牌使用）。
		await PowerCmd.Apply<ArtTrackerPower>(choiceContext, owner.Creature, 1m, owner.Creature, null);

		return card;
	}

	// 随机挑选并创建一张术式实例（含可选升级），不放入任何牌堆。
	// 候选过滤逻辑（术式-凋零在全员已缓慢时排除）与 CreateRandomInHand 的 doc 注释保持一致，
	// 直接打出流程与加手牌流程共用此处，避免两处随机池漂移。
	private static CardModel CreateRandomArtCard(Player owner, ICombatState combatState, Rng rng, bool upgraded)
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
		return card;
	}

	/// <summary>
	/// 生成一张随机术式并直接快速自动打出（路径追踪 / 附魔挖掘 / 术法归元共用的快节奏流程）。
	/// 不再保留「进手牌」动画：术式模型静默进手牌（skipVisuals，不播放飞入/展牌动画），
	/// 卡牌节点直接出现在打出区，自动打出时跳过牌堆移动/等待动画（skipCardPileVisuals），
	/// 只保留术式自身的攻击/命中动画，看起来是效果“当场施放术式”。
	/// 与 <see cref="CreateRandomInHand"/> 共用随机/升级/生成钩子/ArtTrackerPower 计数逻辑：
	/// 随机选择、术式+、万物通元随机附魔与术式召回等 AfterCardGeneratedForCombat 钩子不受影响。
	/// </summary>
	/// <param name="beforeAutoPlay">自动打出开始前回调（可用于防递归登记正在打出的术式）。</param>
	/// <param name="afterAutoPlay">自动打出结束后回调（可用于解除防递归登记）。</param>
	public static async Task<CardModel> CreateRandomArtAndAutoPlay(
		Player owner,
		ICombatState combatState,
		Rng rng,
		PlayerChoiceContext choiceContext,
		Player? creator = null,
		bool upgraded = false,
		Action<CardModel>? beforeAutoPlay = null,
		Action<CardModel>? afterAutoPlay = null)
	{
		// 生成术式实例（不放入任何牌堆；候选过滤/升级与加手牌路径完全一致）。
		CardModel card = CreateRandomArtCard(owner, combatState, rng, upgraded);

		Player actualCreator = creator ?? owner;

		// 静默加入手牌（不播放进手牌动画）。核心 AddGeneratedCardsToCombat 不暴露 skipVisuals，
		// 这里按与其相同的顺序复刻：战斗历史 CardGenerated → Add(skipVisuals:true) → AfterCardGeneratedForCombat 钩子，
		// 以保证生成记录与“生成术式”监听（万物通元随机附魔、术式召回加回响等）仍然生效。
		CombatManager.Instance.History.CardGenerated(combatState, card, actualCreator);
		await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Bottom, clonedBy: null, skipVisuals: true);
		await Hook.AfterCardGeneratedForCombat(combatState, card, actualCreator);

		// 记录本场战斗为该玩家生成的术式数量 +1（供“造成生成数伤害”的卡牌使用）。
		await PowerCmd.Apply<ArtTrackerPower>(choiceContext, owner.Creature, 1m, owner.Creature, null);

		// 卡牌节点直接出现在打出区（不经过手牌），让玩家看到“正在打出的术式”。
		// 节点创建失败（如 TestMode）时退回纯模型流程，不残留 UI。
		bool shownInPlayArea = ShowArtInPlayArea(card);

		beforeAutoPlay?.Invoke(card);

		// 自动打出：跳过牌堆移动/等待动画（省去手牌→打出区 tween、0.25~0.35s 固定等待与打出区→弃牌 tween），
		// 只保留术式自身的攻击/命中动画，避免连续多张术式时动画逐张拖沓。
		await CardCmd.AutoPlay(choiceContext, card, null, skipCardPileVisuals: true);

		afterAutoPlay?.Invoke(card);

		// skipCardPileVisuals 不会清理卡牌节点：手动移除打出区残留的节点，否则会卡在 UI 中。
		if (shownInPlayArea)
		{
			RemovePlayAreaNode(card);
		}

		return card;
	}

	// 把术式的卡牌节点直接放到打出区目标位置（不做进手牌动画，卡牌原地出现在打出区）。
	private static bool ShowArtInPlayArea(CardModel card)
	{
		NCombatRoom? combatRoom = NCombatRoom.Instance;
		if (combatRoom == null)
		{
			return false;
		}
		NCard? node = NCard.Create(card);
		if (node == null)
		{
			return false;
		}

		// 直接挂到打出区并按其布局定位；卡牌在自动打出期间保持在打出区展示。
		combatRoom.Ui.AddToPlayContainer(node);
		node.UpdateVisuals(PileType.Play, CardPreviewMode.Normal);
		node.Scale = Vector2.One * 0.8f;
		node.Position = PileType.Play.GetTargetPosition(node);
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
