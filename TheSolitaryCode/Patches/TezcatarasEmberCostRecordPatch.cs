using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Patching.Models;

namespace TheSolitary.Patches;

// 特兹卡塔拉的余烬（TezcatarasEmber）"附魔前费用"记录补丁。
//
// 背景：原版余烬的降费是不可逆的永久改写——OnEnchant 里调用
//   Card.EnergyCost.UpgradeBy(-Card.EnergyCost.GetWithModifiers(CostModifiers.None))
// 直接把卡牌基础费用 _base 永久改写成 0（与真实升级同一条路径），
// 而游戏清除附魔（CardModel.ClearEnchantmentInternal）只摘除附魔引用、不恢复费用，
// EnchantmentModel 也没有任何"移除附魔"回调。
//
// 原版没有机制会移除余烬，因此这个副作用永远碰不到；
// 但本 Mod 的「轮回」「换位」会在 EnchantHelpers 里 ClearEnchantment 交换附魔，
// 于是被移除的余烬会把 0 费留在原卡上。
//
// 本补丁在 OnEnchant 执行前（Prefix）快照"附魔前的有效基础费用"（含真实升级），
// 在 Postfix 写入附魔的 Props，供 EnchantHelpers 在清除余烬时恢复原费用。
public sealed class TezcatarasEmberCostRecordPatch : IPatchMethod
{
	// Props 键名（EnchantHelpers.TryGetEmberOriginalCost 按此读取）。
	public const string OriginalCostPropName = "TezcatarasEmberOriginalCost";

	// 补丁 ID（RitsuLib 要求全局唯一）。
	public static string PatchId => "thesolitary_tezcataras_ember_cost_record";

	public static string Description =>
		"Record the card's pre-enchant energy cost in TezcatarasEmber's Props so that clearing the enchantment can restore it";

	// 非关键补丁：若游戏更新导致 TezcatarasEmber.OnEnchant 签名变化，仅本修复失效，不影响整个 Mod。
	public static bool IsCritical => false;

	public static ModPatchTarget[] GetTargets() =>
	[
		// OnEnchant 是 protected override、无参数；RitsuLib 用 Instance|NonPublic 解析，可以命中。
		new ModPatchTarget(typeof(TezcatarasEmber), "OnEnchant", [], ignoreIfMissing: true)
	];

	// 附魔前快照：OnEnchant 执行前，费用尚未被改写为 0。
	// 注意 CardCmd.Enchant 中先 EnchantInternal（绑定 __instance.Card）再 ModifyCard() → OnEnchant，
	// 因此此时 __instance.Card 已可用且必为 mutable 实例。
	public static void Prefix(EnchantmentModel __instance, out int __state)
	{
		__state = __instance.HasCard && __instance.Card.IsMutable
			? __instance.Card.EnergyCost.GetWithModifiers(CostModifiers.None)
			: -1;
	}

	// 把附魔前费用写入附魔的 Props（随附魔实例在内存中存活；余烬只存在于本场战斗的手牌克隆上，
	// 不清算序列化问题）。记录失败只影响"移除余烬后恢复费用"，不应影响游戏，故捕获异常。
	public static void Postfix(EnchantmentModel __instance, int __state)
	{
		try
		{
			if (__state < 0)
			{
				return;
			}
			SavedProperties props = __instance.Props ??= new SavedProperties();
			props.ints ??= [];
			props.ints.Add(new SavedProperties.SavedProperty<int>(OriginalCostPropName, __state));
		}
		catch (Exception ex)
		{
			Entry.Logger.Error(ex.ToString());
		}
	}
}
