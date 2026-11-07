using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Random;

namespace TheSolitary.Enchantments;

// 共享随机附魔池（character.org 的随机附魔池）：锋利 / 动量 / 本能 / 涡旋 / 伶俐 / 灵巧。
// 供唤醒、维度调和及凝华战后奖励等随机附魔来源复用。
public static class RandomEnchantPool
{
	/// <summary>
	/// 随机附魔池条目：CanEnchant 判断可用性，Apply 施加附魔，Amount 为该附魔的数值。
	/// </summary>
	private sealed record Entry(Func<CardModel, bool> CanEnchant, Action<CardModel, decimal> Apply, decimal Amount);

	// character.org 随机附魔池。
	private static readonly Entry[] Pool =
	[
		new(EnchantHelpers.CanEnchant<Sharp>, ApplyEnchant<Sharp>, 3m),
		new(EnchantHelpers.CanEnchant<Momentum>, ApplyEnchant<Momentum>, 3m),
		new(EnchantHelpers.CanEnchant<Instinct>, ApplyEnchant<Instinct>, 1m),
		new(EnchantHelpers.CanEnchant<Spiral>, ApplyEnchant<Spiral>, 1m),
		new(EnchantHelpers.CanEnchant<Adroit>, ApplyEnchant<Adroit>, 2m),
		new(EnchantHelpers.CanEnchant<Nimble>, ApplyEnchant<Nimble>, 2m)
	];

	/// <summary>
	/// 从随机附魔池中挑一个对该牌生效的附魔并施加；
	/// 若没有任何附魔能作用于该牌（例如状态/诅咒牌）则无事发生。
	/// </summary>
	public static void EnchantRandomly(Rng rng, CardModel target)
	{
		List<Entry> valid = Pool.Where(entry => entry.CanEnchant(target)).ToList();
		if (valid.Count == 0)
		{
			return;
		}

		Entry? pick = rng.NextItem(valid);
		if (pick != null)
		{
			pick.Apply(target, pick.Amount);
		}
	}

	/// <summary>
	/// 该牌能否被随机附魔池中至少一种附魔作用。
	/// 供随机附魔候选筛选使用：除跳过已附魔的牌外，还要跳过状态/诅咒等无法附魔的牌。
	/// </summary>
	public static bool CanEnchantRandomly(CardModel card)
	{
		return Pool.Any(entry => entry.CanEnchant(card));
	}

	/// <summary>
	/// 对目标牌施加指定附魔。
	/// </summary>
	private static void ApplyEnchant<T>(CardModel card, decimal amount) where T : EnchantmentModel
	{
		CardCmd.Enchant<T>(card, amount);
	}
}
