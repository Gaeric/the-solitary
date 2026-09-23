using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using TheSolitary.Patches;

namespace TheSolitary.Cards;

// 共享工具：按附魔牌数量缩放效果的卡牌公用逻辑。
// 供风暴 EnchantStorm、共轭 Conjugate 等卡牌复用，避免两处统计逻辑漂移。
public static class EnchantHelpers
{
	/// <summary>
	/// 获取玩家当前所有战斗牌堆中的卡牌（手牌 / 抽牌堆 / 弃牌堆 / 消耗堆 / 打出堆）。
	/// 战斗中牌组 Deck 的牌会以克隆形式存在于上述牌堆中，牌组原件通过 DeckVersion 引用回原牌，
	/// 因此无需再单独遍历 Deck 堆，避免重复统计/重复处理。
	/// </summary>
	public static IEnumerable<CardModel> GetAllCombatPileCards(Player player)
	{
		// 战斗牌堆（手牌/抽牌堆/弃牌堆/消耗堆/打出堆）只存在于战斗期间：
		// 非战斗时 CardPile.Get 返回 null，而 PileType.GetPile 会直接抛
		// "Tried to get X pile while out of combat."。
		// 牌库界面等非战斗预览（共轭 Conjugate / 风暴 EnchantStorm 的预览动态变量）
		// 也会调用本方法，因此必须跳过 null 牌堆——战斗外安全返回空集合（张数计为 0）。
		foreach (PileType pileType in Enum.GetValues<PileType>())
		{
			if (!pileType.IsCombatPile())
			{
				continue;
			}
			CardPile? pile = CardPile.Get(pileType, player);
			if (pile == null)
			{
				continue;
			}
			foreach (CardModel card in pile.Cards)
			{
				yield return card;
			}
		}
	}

	/// <summary>
	/// 统计当前所有牌堆中的附魔牌数量（参考灰烬打击 AshenStrike 用 PileType.GetPile 访问牌堆的方式）。
	/// 当前所有牌堆 = 手牌 / 抽牌堆 / 弃牌堆 / 消耗堆 / 打出堆。
	/// 战斗中运行牌组 Deck 的牌会以克隆形式存在于上述牌堆中，因此不额外统计 Deck，避免重复计数。
	/// 战斗外（牌库界面等）不存在战斗牌堆，返回 0。
	/// </summary>
	public static int CountEnchantedCardsInAllPiles(Player player)
	{
		return GetAllCombatPileCards(player).Count(card => card.Enchantment != null);
	}

	/// <summary>
	/// 判断一张牌是否带有数值型附魔——即附魔自身携带一个有实际含义的 <see cref="EnchantmentModel.Amount"/>。
	/// 判断依据：<see cref="EnchantmentModel.ShowAmount"/>（默认 false）。
	/// 原版中 ShowAmount 为 true 的附魔：伶俐 Adroit / 动量 Momentum / 灵巧 Nimble / 锋利 Sharp / 迅速 Swift / 活力 Vigorous，
	/// 它们的 Amount 会显示在卡面的附魔角标上，且效果强度由 Amount 决定。
	/// 涡旋 Spiral / 荣光 Glam 等使用固定 DynamicVar 的附魔为 false——它们不读取 Amount，
	/// 增大其 Amount 没有任何效果，因此不应计入。
	/// </summary>
	public static bool HasValueEnchantment(CardModel card)
	{
		return card.Enchantment != null && card.Enchantment.ShowAmount;
	}

	/// <summary>
	/// 使一张牌的附魔数值 +amount（新增机制，character.org 白卡 #15「使一张手牌的附魔数值+1」）。
	/// 注意：「数值」专指附魔自身的 <see cref="EnchantmentModel.Amount"/>（卡面上附魔角标显示的值，
	/// 例如锋利/灵巧的加成量、迅速的抽牌量）——本方法只递增附魔的 Amount，
	/// 不会改动卡牌本身的伤害/格挡等任何数值。
	/// 实现参考原版 黏糊 Goopy.AfterCardPlayed：
	/// 1. 当前战斗副本的附魔 Amount +amount（本场战斗立即生效）；
	/// 2. 若 <paramref name="persistToDeckVersion"/> 为 true（默认），且该牌来自牌组
	///    （DeckVersion 非空且带附魔），同步递增牌组版本的 Amount，使加成跨战斗永久生效；
	///    传 false 则只作用于本场战斗。
	/// 若该牌没有数值型附魔，则无事发生。
	/// </summary>
	public static void IncreaseEnchantmentValue(CardModel card, int amount = 1, bool persistToDeckVersion = true)
	{
		EnchantmentModel? enchantment = card.Enchantment;
		if (enchantment == null || !enchantment.ShowAmount)
		{
			return;
		}

		// 1. 当前战斗副本的附魔 Amount +amount。
		enchantment.Amount += amount;

		// 2. 可选：同步递增牌组版本 Amount，使加成跨战斗永久生效（参考 Goopy.AfterCardPlayed 对 DeckVersion 的处理）。
		//    默认开启；仅希望本场战斗临时生效时传 false。
		if (persistToDeckVersion && card.DeckVersion?.Enchantment != null)
		{
			card.DeckVersion.Enchantment.Amount += amount;
		}
	}

	/// <summary>
	/// 从手牌中选择两张牌并交换它们的附魔（蓝卡 #3 轮回 的完整交换逻辑，抽离为共享方法）。
	/// 供轮回 SwapEnchantments 技能与蓝卡 #26 能力牌（回合开始时触发）复用。
	/// 选择不足两张（取消选择 / 手牌不足两张）则无事发生；
	/// 手牌没有附魔牌时跳过选择过程；选中的两张牌都没有附魔时跳过交换。
	/// 直接在两张原牌实例上交换附魔：先快照附魔为全新实例（初始运行状态：Status 复位、
	/// 一次性标记清除），清除原附魔后施加另一张的附魔，例如已触发的活力/荣光交换后会「重新充能」。
	/// 不重建卡牌（不使用 CardCmd.Transform），以保留卡牌自身的本场战斗状态（如掌中奇术的减费）。
	/// </summary>
	public static async Task SwapEnchantmentsBetweenTwoHandCards(
		PlayerChoiceContext choiceContext,
		Player player,
		CardSelectorPrefs prefs,
		AbstractModel source)
	{
		// 手里没有任何附魔牌时，交换没有意义，直接跳过选择过程。
		if (player.PlayerCombatState!.Hand.Cards.All(card => card.Enchantment == null))
		{
			return;
		}

		List<CardModel> selection = (await CardSelectCmd.FromHand(
			prefs: prefs,
			context: choiceContext,
			player: player,
			filter: null,
			source: source)).ToList();

		if (selection.Count < 2)
		{
			return;
		}

		// 直接在两牌原实例上交换附魔（先快照为全新实例，再清除并无条件施加），
		// 使交换后的附魔「重新充能」。
		SwapEnchantmentsBetweenTwoCards(selection[0], selection[1]);
	}

	/// <summary>
	/// 将一张指定牌（例如正在打出的卡牌本身）与手牌中一张牌交换附魔（白卡 拟合 的交换逻辑）。
	/// 指定牌与手牌都没有附魔时跳过选择过程；取消选择则无事发生。
	/// 交换逻辑与两牌交换完全一致：先快照为全新实例（Status 复位、一次性标记清除），
	/// 清除原附魔后无条件施加另一张的附魔，使交换后的附魔「重新充能」。
	/// </summary>
	/// <param name="choiceContext">选择上下文。</param>
	/// <param name="player">玩家。</param>
	/// <param name="thisCard">参与交换的指定牌（一般为当前打出的卡牌实例）。</param>
	/// <param name="prefs">选择偏好（数量固定 1）。</param>
	/// <param name="source">效果来源（用于选择界面）。</param>
	public static async Task SwapEnchantmentWithHandCard(
		PlayerChoiceContext choiceContext,
		Player player,
		CardModel thisCard,
		CardSelectorPrefs prefs,
		AbstractModel source)
	{
		// 本卡与手牌都没有附魔时，交换没有意义，直接跳过选择过程。
		if (thisCard.Enchantment == null && player.PlayerCombatState!.Hand.Cards.All(card => card.Enchantment == null))
		{
			return;
		}

		CardModel? picked = (await CardSelectCmd.FromHand(
			prefs: prefs,
			context: choiceContext,
			player: player,
			filter: null,
			source: source)).FirstOrDefault();

		// 取消选择则无事发生。
		if (picked == null)
		{
			return;
		}

		SwapEnchantmentsBetweenTwoCards(thisCard, picked);
	}

	/// <summary>
	/// 直接在两牌原实例上交换附魔（先快照为全新实例，再清除并无条件施加，不重建卡牌）：
	/// 附魔对卡牌数值的贡献是钩子式的（EnchantXAdditive / 自带 DynamicVars），清除后自动失效，
	/// 无需 CardCmd.Transform 重建——重建会丢失卡牌自身的本场战斗状态（如掌中奇术的减费），
	/// 也不会再触发生成牌钩子，因此附魔造物不会误触发。
	/// 施加后播放原版附魔特效（NCardEnchantVfx）作为简单视觉反馈。
	/// </summary>
	private static void SwapEnchantmentsBetweenTwoCards(CardModel first, CardModel second)
	{
		// 两张被选牌都没有附魔时，交换没有意义，跳过后续所有过程。
		if (first.Enchantment == null && second.Enchantment == null)
		{
			return;
		}

		// 先快照两张牌的附魔为全新实例（初始运行状态：Status 复位、一次性标记清除），
		// 使交换后的附魔「重新充能」。
		EnchantmentModel? firstEnchantment = RebuildEnchantment(first.Enchantment);
		EnchantmentModel? secondEnchantment = RebuildEnchantment(second.Enchantment);

		// 附魔对卡牌的改写（费用/关键词）不会随附魔被清除而撤销（游戏没有"移除附魔"回调），
		// 因此在清除前快照两张牌各自需要还原的副作用，清除后再逐项还原。
		RemovedEnchantmentSideEffects firstEffects = RemovedEnchantmentSideEffects.Capture(first.Enchantment);
		RemovedEnchantmentSideEffects secondEffects = RemovedEnchantmentSideEffects.Capture(second.Enchantment);

		// 直接在两牌原实例上清除并施加附魔（不重建卡牌）。
		CardCmd.ClearEnchantment(first);
		CardCmd.ClearEnchantment(second);

		// 还原被移除附魔留下的改写：余烬的降费、灵魂之力移除的消耗、其它附魔添加的关键词等。
		firstEffects.Restore(first);
		secondEffects.Restore(second);

		// 无条件施加交换后的附魔（与游戏加载时重新施加附魔一致，绕过 CanEnchant）。
		// 施加后播放原版附魔特效（NCardEnchantVfx）作为简单视觉反馈。
		if (secondEnchantment != null)
		{
			ApplyEnchantment(first, secondEnchantment, secondEnchantment.Amount);
			PlayEnchantVfx(first);
		}
		if (firstEnchantment != null)
		{
			ApplyEnchantment(second, firstEnchantment, firstEnchantment.Amount);
			PlayEnchantVfx(second);
		}
	}

	/// <summary>
	/// 读取余烬附魔记录下的"附魔前费用"（由 TezcatarasEmberCostRecordPatch 在施加时写入 Props）。
	/// 只有被清除的是余烬且记录存在时才返回 true，否则不恢复（保持旧行为）。
	/// </summary>
	private static bool TryGetEmberOriginalCost(EnchantmentModel? enchantment, out int originalCost)
	{
		originalCost = -1;
		if (enchantment is not TezcatarasEmber || enchantment.Props?.ints == null)
		{
			return false;
		}
		foreach (SavedProperties.SavedProperty<int> prop in enchantment.Props.ints)
		{
			if (prop.name == TezcatarasEmberCostRecordPatch.OriginalCostPropName)
			{
				originalCost = prop.value;
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// 读取附魔记录下的"附魔前的本地关键词集合"（由 EnchantKeywordRecordPatch 在 ModifyCard → OnEnchant 之前写入 Props）。
	/// 附魔的 OnEnchant 会永久改写卡牌关键词——灵魂之力 SoulsPower 移除消耗 Exhaust，
	/// 黏糊 Goopy / 稳定 Steady / 御准 RoyallyApproved / 余烬 TezcatarasEmber 添加关键词——
	/// 而清除附魔不会撤销改写，因此需要这份快照来还原。
	/// 没有记录时返回 false（保持旧行为：不还原关键词）。
	/// </summary>
	private static bool TryGetKeywordsBeforeEnchant(EnchantmentModel? enchantment, out HashSet<CardKeyword> keywordsBefore)
	{
		keywordsBefore = [];
		if (enchantment?.Props?.intArrays == null)
		{
			return false;
		}
		foreach (SavedProperties.SavedProperty<int[]> prop in enchantment.Props.intArrays)
		{
			if (prop.name != EnchantKeywordRecordPatch.KeywordsBeforeEnchantPropName || prop.value == null)
			{
				continue;
			}
			foreach (int value in prop.value)
			{
				keywordsBefore.Add((CardKeyword)value);
			}
			return true;
		}
		return false;
	}

	/// <summary>
	/// 附魔被移除后还原它改写过的卡牌关键词：把"附魔前快照"与"当前关键词"双向求差。
	/// - 快照里有、现在没有 → 是附魔移除掉的（灵魂之力 → 消耗 Exhaust）→ 加回来；
	/// - 现在有、快照里没有 → 是附魔添加的（黏糊 → 消耗；稳定/御准 → 保留、固有；余烬 → 永恒）→ 去掉。
	/// CardModel.AddKeyword / RemoveKeyword 只作用于 LocalKeywords（canonical + 本地增删），
	/// 不涉及全局关键词，因此不会影响其它牌或战斗中由 Power 提供的关键词。
	/// </summary>
	private static void RestoreCardKeywordsAfterEnchantmentRemoved(CardModel card, HashSet<CardKeyword> keywordsBefore)
	{
		// GetKeywordsWithSources(Local) 在本地请求时返回卡牌内部的实时集合，必须复制后再迭代。
		HashSet<CardKeyword> keywordsNow = card.GetKeywordsWithSources(KeywordSources.Local).ToHashSet();

		foreach (CardKeyword keyword in keywordsBefore.Except(keywordsNow))
		{
			card.AddKeyword(keyword);
		}
		foreach (CardKeyword keyword in keywordsNow.Except(keywordsBefore))
		{
			card.RemoveKeyword(keyword);
		}
	}

	/// <summary>
	/// 一张牌被移除附魔后需要手动撤销的"卡牌永久改写"。
	/// 游戏清除附魔（CardCmd.ClearEnchantment → CardModel.ClearEnchantmentInternal → EnchantmentModel.ClearInternal）
	/// 只摘除附魔引用，EnchantmentModel 也没有任何"移除附魔"回调，因此 OnEnchant 的改写会残留在原卡上：
	/// - 灵魂之力 SoulsPower 移除消耗 Exhaust（快照见 EnchantKeywordRecordPatch）；
	/// - 黏糊 / 稳定 / 御准 / 余烬添加关键词，余烬还会把基础费用永久改写成 0
	///   （费用记录见 TezcatarasEmberCostRecordPatch）。
	/// 交换附魔前用 <see cref="Capture"/> 快照，清除后调用 <see cref="Restore"/> 还原。
	/// </summary>
	private readonly struct RemovedEnchantmentSideEffects
	{
		// 需要还原的余烬费用（-1 = 无需还原）。
		private readonly int _emberOriginalCost;

		// 附魔前的本地关键词快照（null = 没有记录，不还原关键词）。
		private readonly HashSet<CardKeyword>? _keywordsBefore;

		private RemovedEnchantmentSideEffects(int emberOriginalCost, HashSet<CardKeyword>? keywordsBefore)
		{
			_emberOriginalCost = emberOriginalCost;
			_keywordsBefore = keywordsBefore;
		}

		/// <summary>快照一张牌当前附魔在移除后会留下的副作用（必须在 ClearEnchantment 之前调用）。</summary>
		public static RemovedEnchantmentSideEffects Capture(EnchantmentModel? enchantment)
		{
			// TryGetEmberOriginalCost 为 false 时 out 值固定为 -1，表示"无需还原费用"。
			bool hasEmberCost = TryGetEmberOriginalCost(enchantment, out int emberOriginalCost);
			HashSet<CardKeyword>? keywordsBefore = null;
			if (enchantment != null && TryGetKeywordsBeforeEnchant(enchantment, out HashSet<CardKeyword> snapshot))
			{
				keywordsBefore = snapshot;
			}
			return new RemovedEnchantmentSideEffects(hasEmberCost ? emberOriginalCost : -1, keywordsBefore);
		}

		/// <summary>撤销这些副作用（必须在附魔已被移除后调用）。</summary>
		public void Restore(CardModel card)
		{
			if (_emberOriginalCost >= 0)
			{
				RestoreCardAfterEmberRemoved(card, _emberOriginalCost);
			}
			if (_keywordsBefore != null)
			{
				RestoreCardKeywordsAfterEnchantmentRemoved(card, _keywordsBefore);
			}
		}
	}

	/// <summary>
	/// 余烬被移除后恢复卡牌：把基础费用还原为附魔前的值。
	/// 余烬加上的永恒（Eternal）关键词改由 <see cref="RestoreCardKeywordsAfterEnchantmentRemoved"/>
	/// 按"附魔前关键词快照"还原——这里不再无条件 RemoveKeyword(Eternal)：
	/// RemoveKeyword 直接作用于 LocalKeywords（= CanonicalKeywords + AddKeyword - RemoveKeyword），
	/// 因此对"自带永恒"的牌（如 黏糊魔典 StickyGrimoire 的 CanonicalKeywords）同样会移除，
	/// 反而会误伤卡牌自身的永恒。
	/// </summary>
	private static void RestoreCardAfterEmberRemoved(CardModel card, int originalCost)
	{
		if (originalCost >= 0)
		{
			card.EnergyCost.SetCustomBaseCost(originalCost);
		}
	}

	/// <summary>
	/// 从序列化形式重建附魔，使交换后的副本拥有初始运行状态（Status 复位、一次性标记清除），
	/// 同时保留 Id、Amount 与各 [SavedProperty] 属性。
	/// 注意：附魔自身挂在模型上的 <see cref="EnchantmentModel.Props"/> 记录不会随重建保留
	/// （SavedProperties.From 只收集 [SavedProperty] 属性），因此重建后的实例会在
	/// ApplyEnchantment → ModifyCard → OnEnchant 时由补丁重新写入"附魔前"记录。
	/// </summary>
	private static EnchantmentModel? RebuildEnchantment(EnchantmentModel? enchantment)
	{
		if (enchantment == null)
		{
			return null;
		}
		return EnchantmentModel.FromSerializable(enchantment.ToSerializable());
	}

	/// <summary>
	/// 施加附魔的内部路径（与 CardCmd.Enchant 在 EnchantInternal 之后的步骤一致，但绕过 CanEnchant，
	/// 因为交换是无条件的）。
	/// </summary>
	private static void ApplyEnchantment(CardModel card, EnchantmentModel enchantment, decimal amount)
	{
		card.EnchantInternal(enchantment, amount);
		enchantment.ModifyCard();
		card.FinalizeUpgradeInternal();
	}

	/// <summary>
	/// 绕过 <see cref="EnchantmentModel.CanEnchant"/> 直接给指定卡牌施加附魔，用于"把牌变换成某种牌后
	/// 再附魔"这类目标由效果指定的情况（例：博采众长把牌变换成能力牌后附魔 注能 Imbued，
	/// 而注能自身只允许附魔技能牌）。附魔实例需要是可变实例（<c>ModelDb.Enchantment&lt;T&gt;().ToMutable()</c>）。
	/// </summary>
	/// <param name="card">要附魔的牌（必须是可变实例）。</param>
	/// <param name="enchantment">要施加的附魔（可变实例）。</param>
	/// <param name="amount">附魔数值（默认 1，与 CardCmd.Enchant 的调用方式一致）。</param>
	public static void ApplyEnchantmentBypassingCanEnchant(CardModel card, EnchantmentModel enchantment, decimal amount = 1m)
	{
		ApplyEnchantment(card, enchantment, amount);
	}

	/// <summary>
	/// 播放原版附魔特效（NCardEnchantVfx）作为交换后的简单视觉反馈。
	/// 原版动画内部固定约 2 秒（1s 补间 + 1s 等待），这里只展示约 0.5 秒后快速缩走，
	/// 避免拖慢交换节奏。卡牌无附魔时跳过（特效需要读取附魔图标）。
	/// </summary>
	private static void PlayEnchantVfx(CardModel card)
	{
		if (card.Enchantment == null)
		{
			return;
		}
		NCardEnchantVfx? vfx = NCardEnchantVfx.Create(card);
		if (vfx == null)
		{
			return;
		}
		NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(vfx);
		TaskHelper.RunSafely(FadeOutAndFree(vfx));
	}

	/// <summary>
	/// 短暂展示后快速缩走并释放附魔特效。
	/// </summary>
	private static async Task FadeOutAndFree(NCardEnchantVfx vfx)
	{
		await Cmd.Wait(0.5f);
		if (!GodotObject.IsInstanceValid(vfx))
		{
			return;
		}
		Tween tween = vfx.CreateTween();
		tween.TweenProperty(vfx, "scale", Vector2.Zero, 0.15f).SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
		await tween.AwaitFinished(vfx);
		vfx.QueueFreeSafely();
	}
}

