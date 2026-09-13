using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Patching.Models;

namespace TheSolitary.Patches;

// 墨影（Inky）目标补丁：把"每命中一次加一层虚弱"补成原版没有的样子。
//
// 原版问题（MegaCrit.Sts2.Core.Models.Enchantments.Inky.OnPlay，已对游戏安装目录 sts2.dll 反编译核实）：
//   if (Card.TargetType != TargetType.AllEnemies) targets = [cardPlay.Target];
//   else                                          targets = Card.CombatState.HittableEnemies;
//   await PowerCmd.Apply<WeakPower>(choiceContext, targets, base.DynamicVars.Weak.BaseValue, ...);
// ① 只有"全体敌人"牌走 HittableEnemies，其余一律取 cardPlay.Target；而 RandomEnemy（随机多段攻击牌的目标类型：
//    飞剑回旋镖 SwordBoomerang、弹跳飞刀 Ricochet、高射炮 FlakCannon、星尘 Stardust，本 Mod 的光子映射等）
//    以及 Self / None / AnyPlayer 等牌不选目标，cardPlay.Target 为 null → [null] 进 PowerCmd.Apply →
//    target.CanReceivePowers 抛 NullReferenceException（原版只由 BladeOfInk 发给 AnyEnemy/AllEnemies 的小刀，没被踩到）。
// ② 原版每次打牌最多只施加 1 层（每个目标一次），无法表达"每命中一次加一层虚弱"
//    （指定敌人的多段牌打同一敌人 3 次，原版也只有 1 层）。
//
// 本补丁（配合 InkyHitRecordPatch）按命中次数施加：
//   ① 本次打牌有命中登记（无论这张牌的目标类型是什么）→ 命中几次就对命中的敌人施加几次 1 层虚弱
//      （同一敌人被多段打中 → 叠成多层），跳过原版；
//   ② 有目标但本次没造成伤害（例如附了墨影的技能牌）→ 原版照常：给该目标 1 层；
//   ③ 全体敌人牌且本次没造成伤害 → 原版照常：所有可命中敌人各 1 层；
//   ④ 无目标、没造成伤害的随机敌人牌 → 兜底给随机一个可命中敌人（走原版给 1 层），不让墨影完全落空；
//   ⑤ 其余无目标牌（Self / None / AnyPlayer / TargetedNoCreature / Osty 等）→ 安全跳过（原版会对 null 抛 NRE）。
public sealed class InkyTargetPatch : IPatchMethod
{
	// 补丁 ID（RitsuLib 要求全局唯一）。
	public static string PatchId => "thesolitary_inky_target";

	public static string Description =>
		"Make Inky (墨影) apply 1 Weak per hit (stacking on multi-hit cards) and never crash on cards without a selected target";

	// 非关键补丁：若游戏更新导致 Inky.OnPlay 签名变化，仅本补丁失效（退回原版，最坏情况仍是原 NRE）。
	public static bool IsCritical => false;

	public static ModPatchTarget[] GetTargets() =>
	[
		// Inky.OnPlay 是 public override async Task，参数为 (PlayerChoiceContext, CardPlay?)；
		// 显式给出参数类型列表，避免 GetMethod 命中歧义。
		new ModPatchTarget(
			typeof(Inky),
			nameof(EnchantmentModel.OnPlay),
			[typeof(PlayerChoiceContext), typeof(CardPlay)],
			ignoreIfMissing: true)
	];

	public static bool Prefix(EnchantmentModel __instance, ref CardPlay? cardPlay, ref Task __result)
	{
		try
		{
			CardModel? card = __instance.HasCard ? __instance.Card : null;

			// ① 本次打牌有命中登记：逐命中施加（每命中一次加 1 层），跳过原版。
			//    这一条放在最前面，所以"指定敌人的多段牌"（AnyEnemy）也会按段数叠虚弱。
			if (card != null
				&& cardPlay != null
				&& InkyHitRegistry.TryConsume(card, cardPlay.PlayIndex, out List<Creature> hits))
			{
				__result = ApplyWeakPerHitAsync(__instance, card, hits);
				return false;
			}

			// ② 有目标但本次没造成伤害：原版逻辑与数值照常。
			if (cardPlay?.Target != null)
			{
				return true;
			}

			// ③ 全体敌人牌但本次没造成伤害：原版分支用 HittableEnemies，不解引用 target，放行。
			if (card is { TargetType: TargetType.AllEnemies })
			{
				return true;
			}

			// ④ 无目标、没造成伤害的随机敌人牌：兜底给随机一个可命中敌人（仍由原版施加 1 层）。
			if (card is { TargetType: TargetType.RandomEnemy }
				&& cardPlay != null
				&& TryPickRandomEnemy(card) is { } fallback)
			{
				cardPlay = CopyWithTarget(cardPlay, fallback);
				return true;
			}

			// ⑤ 其余无目标牌（Self / None / AnyPlayer / …）或没有可用目标：
			//    安全跳过原版（不施加虚弱），并给出已完成的 Task 供调用方 await。
			__result = Task.CompletedTask;
			return false;
		}
		catch (Exception ex)
		{
			// 补丁自身出错时绝不能把异常丢进打牌流程：记日志并安全跳过原版。
			Entry.Logger.Error(ex.ToString());
			__result = Task.CompletedTask;
			return false;
		}
	}

	// 按命中次数逐层施加：集合里命中几次就施加几次 1 层虚弱
	// （同一敌人被多段打中 → 该敌人叠多层；不同敌人各按其被命中的次数叠加）。
	// 返回的 Task 会被写进原方法的 __result，由 CardModel.Play 正常 await ——
	// 即施加发生在本次打牌伤害全部结算完之后，与原版墨影的时机一致（不会反过来增强本次多段伤害）。
	private static async Task ApplyWeakPerHitAsync(EnchantmentModel instance, CardModel card, List<Creature> hits)
	{
		try
		{
			// 施加虚弱不会触发玩家选择，用 ThrowingPlayerChoiceContext（与本 Mod AfterEnchantPatch 同款）。
			PlayerChoiceContext context = new ThrowingPlayerChoiceContext();
			decimal weakPerHit = instance.DynamicVars.Weak.BaseValue;
			foreach (Creature target in hits)
			{
				if (!target.IsAlive)
				{
					continue;
				}

				await PowerCmd.Apply<WeakPower>(context, target, weakPerHit, card.Owner.Creature, card);
			}
		}
		catch (Exception ex)
		{
			// 该 Task 会被打牌流程 await，异常必须自己吞掉，否则会打断整次打牌。
			Entry.Logger.Error(ex.ToString());
		}
	}

	// 给"随机敌人"牌挑一个可命中敌人（与原版 CardCmd.AutoPlay 给 AnyEnemy 牌补目标、
	// WhisperingEarring.GetTarget 同款随机源：RunState.Rng.CombatTargets）。
	// 仅用于"本次没造成伤害"的兜底场景。
	private static Creature? TryPickRandomEnemy(CardModel card)
	{
		ICombatState? combatState = card.CombatState;
		if (combatState == null)
		{
			return null;
		}

		List<Creature> enemies = combatState.HittableEnemies.ToList();
		return enemies.Count switch
		{
			0 => null,
			1 => enemies[0],
			_ => card.Owner.RunState.Rng.CombatTargets.NextItem(enemies),
		};
	}

	// CardPlay.Target 是 required init 属性（外部无法改写），因此复制一份、只替换 Target；
	// 牌 / 玩家 / 资源 / 回牌堆 / 播放序号保持原样，保证原版逻辑一致。
	// 注意：ref 改写只影响 Inky.OnPlay 收到的参数，CardModel.Play 自己的 cardPlay 不受影响。
	private static CardPlay CopyWithTarget(CardPlay source, Creature target)
	{
		return new CardPlay
		{
			Card = source.Card,
			Player = source.Player,
			Target = target,
			ResultPile = source.ResultPile,
			Resources = source.Resources,
			IsAutoPlay = source.IsAutoPlay,
			PlayIndex = source.PlayIndex,
			PlayCount = source.PlayCount,
		};
	}
}
