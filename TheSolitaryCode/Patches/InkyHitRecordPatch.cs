using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Patching.Models;

namespace TheSolitary.Patches;

// 「墨影（Inky）牌本次打牌实际命中过的敌人」登记表 —— 供 InkyTargetPatch 结算时取用。
//
// 为什么需要它：原版 Inky.OnPlay 只知道 cardPlay.Target（或者全体牌时的 HittableEnemies），
// 拿不到"这张牌到底打中了谁、打中几次"。而 RandomEnemy（随机多段攻击牌）根本没有目标，
// 只能靠伤害事件回放：Hook.AfterDamageGiven 每次命中/每个目标都会触发一次（含被完全格挡的命中），
// 参数里带 cardSource（打伤害的牌）与 target（实际承受伤害的敌人）。
//
// 登记键 = (卡牌实例, 本次打牌的 PlayIndex)：
//   - 卡牌实例用 ConditionalWeakTable 弱引用持有，战斗结束卡牌回收后登记自动消失；
//   - PlayIndex 区分 Replay 的多次打牌（每次打牌各自登记、各自消费）。
// 列表**不去重**：墨影的效果是"每命中一次加一层虚弱"，同一敌人被多段打中就要登记多次。
internal static class InkyHitRegistry
{
	private sealed class HitRecords
	{
		// PlayIndex -> 本次打牌命中过的敌人（按命中顺序；命中几次就记几次，同一敌人会出现多次）。
		public Dictionary<int, List<Creature>> ByPlayIndex { get; } = [];
	}

	private static readonly ConditionalWeakTable<CardModel, HitRecords> Records = [];

	// 登记一次命中（每次命中/每个目标调用一次；不做去重）。
	public static void Record(CardModel card, int playIndex, Creature target)
	{
		if (!Records.TryGetValue(card, out HitRecords? records))
		{
			records = new HitRecords();
			Records.Add(card, records);
		}

		if (!records.ByPlayIndex.TryGetValue(playIndex, out List<Creature>? hits))
		{
			hits = [];
			records.ByPlayIndex[playIndex] = hits;
		}

		hits.Add(target);
	}

	// 取走并清空"本次打牌"的命中记录；没有记录（这张牌本次没造成伤害）时返回 false。
	// 注意：只有"无目标牌"分支会消费登记；有目标 / 全体敌人牌仍走原版逻辑，
	// 它们的登记不会被读取（弱引用表会随卡牌实例回收，不构成泄漏）。
	public static bool TryConsume(CardModel card, int playIndex, out List<Creature> hits)
	{
		hits = [];
		if (!Records.TryGetValue(card, out HitRecords? records))
		{
			return false;
		}

		if (!records.ByPlayIndex.Remove(playIndex, out List<Creature>? recorded) || recorded.Count == 0)
		{
			return false;
		}

		hits = recorded;
		return true;
	}
}

// 墨影命中登记补丁：把"墨影牌打出的对敌伤害命中"登记到 InkyHitRegistry。
//
// Hook.AfterDamageGiven 在原版 CreatureCmd.Damage 里对每个 DamageResult 调用一次
// （见 ../sts2_20260821/src/Core/Commands/CreatureCmd.cs:412），
// 因此多段攻击的每一段、随机多段命中的每个敌人都会被逐一登记；完全被格挡的命中也会登记。
public sealed class InkyHitRecordPatch : IPatchMethod
{
	// 补丁 ID（RitsuLib 要求全局唯一）。
	public static string PatchId => "thesolitary_inky_hit_record";

	public static string Description =>
		"Record which enemies each Inky (墨影) enchanted card actually hit, so the enchantment can apply Weak per hit target";

	// 非关键补丁：签名变化时只影响"逐命中上虚弱"的精度（退回 InkyTargetPatch 的随机敌人兜底）。
	public static bool IsCritical => false;

	public static ModPatchTarget[] GetTargets() =>
	[
		// 参数类型按原方法声明顺序给全（可空标注不影响运行时类型）：
		// (PlayerChoiceContext, ICombatState, Creature?, DamageResult, ValueProp, Creature, CardModel?)
		new ModPatchTarget(
			typeof(Hook),
			nameof(Hook.AfterDamageGiven),
			[
				typeof(PlayerChoiceContext),
				typeof(ICombatState),
				typeof(Creature),
				typeof(DamageResult),
				typeof(ValueProp),
				typeof(Creature),
				typeof(CardModel),
			],
			ignoreIfMissing: true)
	];

	// 只登记"墨影牌自己造成的、对敌方的"伤害：
	//   - cardSource 带墨影附魔；
	//   - dealer 就是这张牌拥有者的生物（排除被转移/代为承受的伤害）；
	//   - 目标是敌对方（排除 Osty / 队友代伤、自伤牌）。
	public static void Prefix(Creature? dealer, Creature target, CardModel? cardSource)
	{
		try
		{
			if (cardSource is not { Enchantment: Inky })
			{
				return;
			}

			Creature ownerCreature = cardSource.Owner.Creature;
			if (dealer != ownerCreature || target.Side == ownerCreature.Side)
			{
				return;
			}

			InkyHitRegistry.Record(cardSource, cardSource.CurrentPlayIndex, target);
		}
		catch (Exception ex)
		{
			// 登记失败不应影响战斗流程，记日志即可。
			Entry.Logger.Error(ex.ToString());
		}
	}
}
