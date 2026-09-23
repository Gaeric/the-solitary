using System;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Patching.Models;

namespace TheSolitary.Patches;

// 附魔"改写卡牌关键词"前的快照补丁。
//
// 背景：附魔可以在 OnEnchant 里永久改写卡牌的本地关键词，而游戏清除附魔
// （CardCmd.ClearEnchantment → CardModel.ClearEnchantmentInternal → EnchantmentModel.ClearInternal）
// 只摘除附魔引用，没有 UnmodifyCard / "移除附魔"回调，于是改写会残留在原卡上：
//   - 灵魂之力 SoulsPower：RemoveKeyword(Exhaust) —— 消耗词条被永久移除；
//   - 黏糊 Goopy：AddKeyword(Exhaust)；
//   - 稳定 Steady / 御准 RoyallyApproved：AddKeyword(Retain)（御准还会加 Innate）；
//   - 特兹卡塔拉的余烬 TezcatarasEmber：AddKeyword(Eternal)。
// 原版没有机制会移除附魔，所以这些副作用碰不到；但本 Mod 的「轮回」「拟合」「换位」
// 会在 EnchantHelpers 里 ClearEnchantment 交换附魔，于是副作用会留在被移走附魔的那张牌上。
//
// 本补丁在 EnchantmentModel.ModifyCard（OnEnchant 的唯一调用点，施加附魔/读档重建附魔都会走）
// 执行前，把卡牌"当前的本地关键词集合"写入附魔 Props 作为快照，供 EnchantHelpers
// 在清除该附魔时对比还原（见 EnchantHelpers.RestoreCardKeywordsAfterEnchantmentRemoved）。
//
// 注意：本补丁挂在抽象基类 EnchantmentModel 上，因此对所有附魔（含其它 Mod 的附魔）生效。
public sealed class EnchantKeywordRecordPatch : IPatchMethod
{
	// Props 键名（EnchantHelpers.TryGetKeywordsBeforeEnchant 按此读取）。
	public const string KeywordsBeforeEnchantPropName = "TheSolitaryKeywordsBeforeEnchant";

	// 补丁 ID（RitsuLib 要求全局唯一）。
	public static string PatchId => "thesolitary_enchant_keyword_record";

	public static string Description =>
		"Snapshot a card's local keywords before an enchantment modifies them, so clearing the enchantment can restore them";

	// 非关键补丁：若游戏更新导致 EnchantmentModel.ModifyCard 签名变化，仅本修复失效，不影响整个 Mod。
	public static bool IsCritical => false;

	public static ModPatchTarget[] GetTargets() =>
	[
		// ModifyCard 是 public 实例方法（非 virtual），挂在抽象基类上即可覆盖全部附魔。
		new ModPatchTarget(typeof(EnchantmentModel), "ModifyCard", [], ignoreIfMissing: true)
	];

	// 快照时机：CardCmd.Enchant / CardModel.FromSerializable 都是"先 EnchantInternal 绑定卡牌，
	// 再 ModifyCard() → OnEnchant()"，因此前缀里 Card 已可用、且尚未被本附魔改写。
	public static void Prefix(EnchantmentModel __instance)
	{
		try
		{
			// canonical 实例（图鉴/预览用的只读模型）不写 Props；未绑定卡牌的实例也无从快照
			// （ModifyCard 自身会断言 Card 非空，这里提前避开）。
			if (!__instance.HasCard || !__instance.Card.IsMutable)
			{
				return;
			}

			// 只记录本地关键词（LocalKeywords = CanonicalKeywords + AddKeyword - RemoveKeyword）：
			// 全局关键词由战斗中的其它模型按需计算，不随附魔存续，不需要还原。
			int[] keywordsBefore = __instance.Card
				.GetKeywordsWithSources(KeywordSources.Local)
				.Select(keyword => (int)keyword)
				.OrderBy(value => value)
				.ToArray();

			SavedProperties props = __instance.Props ??= new SavedProperties();
			props.intArrays ??= [];
			// 同一附魔实例可能被反复施加到不同卡牌（本 Mod 交换附魔时会先重建实例再施加），
			// 因此覆盖同名记录而非追加，保证记录始终对应"当前这张牌"。
			props.intArrays.RemoveAll(prop => prop.name == KeywordsBeforeEnchantPropName);
			props.intArrays.Add(new SavedProperties.SavedProperty<int[]>(KeywordsBeforeEnchantPropName, keywordsBefore));
		}
		catch (Exception ex)
		{
			// 快照失败只影响"移除附魔后还原关键词"，不应影响附魔本身，必须吞掉异常。
			Entry.Logger.Error(ex.ToString());
		}
	}
}
