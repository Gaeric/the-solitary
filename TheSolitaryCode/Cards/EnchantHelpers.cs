using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TheSolitary.Cards;

// 共享工具：按附魔牌数量缩放效果的卡牌公用逻辑。
// 供附魔风暴 EnchantStorm、附魔壁垒 EnchantedBulwark 等卡牌复用，避免两处统计逻辑漂移。
public static class EnchantHelpers
{
	/// <summary>
	/// 获取玩家当前所有战斗牌堆中的卡牌（手牌 / 抽牌堆 / 弃牌堆 / 消耗堆 / 打出堆）。
	/// 战斗中牌组 Deck 的牌会以克隆形式存在于上述牌堆中，牌组原件通过 DeckVersion 引用回原牌，
	/// 因此无需再单独遍历 Deck 堆，避免重复统计/重复处理。
	/// </summary>
	public static IEnumerable<CardModel> GetAllCombatPileCards(Player player)
	{
		foreach (PileType pileType in Enum.GetValues<PileType>())
		{
			if (!pileType.IsCombatPile())
			{
				continue;
			}
			foreach (CardModel card in pileType.GetPile(player).Cards)
			{
				yield return card;
			}
		}
	}

	/// <summary>
	/// 统计当前所有牌堆中的附魔牌数量（参考灰烬打击 AshenStrike 用 PileType.GetPile 访问牌堆的方式）。
	/// 当前所有牌堆 = 手牌 / 抽牌堆 / 弃牌堆 / 消耗堆 / 打出堆。
	/// 战斗中运行牌组 Deck 的牌会以克隆形式存在于上述牌堆中，因此不额外统计 Deck，避免重复计数。
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
	/// 从手牌中选择两张牌并交换它们的附魔（蓝卡 #3 交换附魔 的完整交换逻辑，抽离为共享方法）。
	/// 供交换附魔 SwapEnchantments 技能与蓝卡 #26 能力牌（回合开始时触发）复用。
	/// 选择不足两张（取消选择 / 手牌不足两张）则无事发生。
	/// 每张被选牌先还原为未附魔的基础态（保留升级等级，通过 CardCmd.Transform 原位替换），
	/// 再以全新实例（初始运行状态：Status 复位、一次性标记清除）施加另一张牌的附魔，
	/// 例如已触发的活力/荣光交换后会「重新充能」。无附魔的牌直接获得另一张牌的附魔。
	/// </summary>
	public static async Task SwapEnchantmentsBetweenTwoHandCards(
		PlayerChoiceContext choiceContext,
		Player player,
		CardSelectorPrefs prefs,
		AbstractModel source)
	{
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

		CardModel first = selection[0];
		CardModel second = selection[1];

		// 先快照两张牌的附魔为全新实例（初始运行状态），再还原基础态，避免还原丢弃原始附魔。
		EnchantmentModel? firstEnchantment = RebuildEnchantment(first.Enchantment);
		EnchantmentModel? secondEnchantment = RebuildEnchantment(second.Enchantment);

		// 将两张牌还原为未附魔的基础态（保留升级等级）。
		CardModel newFirst = ResetToUnEnchanted(first);
		CardModel newSecond = ResetToUnEnchanted(second);

		// 原位交换（同一牌堆、同一位置）。
		await CardCmd.Transform(
			new CardTransformation[2]
			{
				new CardTransformation(first, newFirst),
				new CardTransformation(second, newSecond)
			},
			null);

		// 无条件施加交换后的附魔（与游戏加载时重新施加附魔一致，绕过 CanEnchant）。
		if (secondEnchantment != null)
		{
			ApplyEnchantment(newFirst, secondEnchantment);
		}
		if (firstEnchantment != null)
		{
			ApplyEnchantment(newSecond, firstEnchantment);
		}
	}

	/// <summary>
	/// 从序列化形式重建附魔，使交换后的副本拥有初始运行状态（Status 复位、一次性标记清除），
	/// 同时保留 Id、Props 与 Amount。
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
	private static void ApplyEnchantment(CardModel card, EnchantmentModel enchantment)
	{
		card.EnchantInternal(enchantment, enchantment.Amount);
		enchantment.ModifyCard();
		card.FinalizeUpgradeInternal();
	}

	/// <summary>
	/// 创建一张未附魔的基础副本（保留升级等级），供 CardCmd.Transform 原位替换。
	/// </summary>
	private static CardModel ResetToUnEnchanted(CardModel original)
	{
		CardModel replacement = original.CardScope!.CreateCard(original.CanonicalInstance, original.Owner);
		replacement.FloorAddedToDeck = original.FloorAddedToDeck;

		// 与 CardModel.FromSerializable 重新施加升级等级的方式一致。
		for (int i = 0; i < original.CurrentUpgradeLevel; i++)
		{
			replacement.UpgradeInternal();
			replacement.FinalizeUpgradeInternal();
		}
		return replacement;
	}
}

