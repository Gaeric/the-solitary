using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace TheSolitary.Cards;

/// <summary>
/// 描述一次被移除的负面效果：种类（Power 的运行时类型）与移除时的数值。
/// 供后续需要按「移除的负面效果」结算的效果使用（例如 character.org 基础牌「每移除一种负面效果获得能量」）。
/// </summary>
public readonly record struct RemovedDebuff(Type Type, int Amount)
{
	/// <summary>该负面效果是否属于指定 Power 类型（如 <see cref="SlowPower"/>）。</summary>
	public bool Is<T>() where T : PowerModel => Type == typeof(T);
}

/// <summary>
/// 负面效果移除共享工具。
/// 将「移除一个角色身上的全部负面效果」抽离成统一入口，并返回被移除的种类与数值，
/// 供净化/涤荡类卡牌以及后续「按移除负面效果结算」的效果复用，避免各处移除逻辑漂移。
/// </summary>
public static class DebuffHelpers
{
	/// <summary>
	/// 移除一个角色身上的全部负面效果，并返回每种被移除负面效果的种类与移除时的数值。
	/// 「负面效果」判定与原版 Rend / Misery 一致：<see cref="PowerModel.TypeForCurrentAmount"/> 为
	/// <see cref="PowerType.Debuff"/> 的 Power（包含临时负面效果实例，如临时力量）。
	/// 逐个调用 <see cref="PowerCmd.Remove(PowerModel)"/>，以播放移除特效并触发 AfterRemoved 钩子。
	/// </summary>
	/// <param name="creature">要移除负面效果的角色。</param>
	/// <returns>被移除的负面效果列表（种类 + 数值）；角色身上没有负面效果时返回空列表。</returns>
	public static async Task<IReadOnlyList<RemovedDebuff>> RemoveAllDebuffs(Creature creature)
	{
		// 先快照当前所有负面效果：PowerCmd.Remove 会修改 creature.Powers 集合，不能边遍历边移除。
		List<PowerModel> debuffs = creature.Powers.Where(IsDebuff).ToList();

		// 在移除前记录种类与数值（移除后 Power 实例已脱离角色，不便再作为返回值使用）。
		List<RemovedDebuff> removed = debuffs
			.Select(power => new RemovedDebuff(power.GetType(), power.Amount))
			.ToList();

		foreach (PowerModel power in debuffs)
		{
			await PowerCmd.Remove(power);
		}

		return removed;
	}

	/// <summary>
	/// 统计一个角色身上的负面效果类型数量（按 Power 的运行时类型去重）。
	/// 判定与原版 Rend / 瘟疫 Pestilence 一致：<see cref="PowerModel.TypeForCurrentAmount"/> 为
	/// <see cref="PowerType.Debuff"/> 的 Power，并排除临时性 Power（<see cref="ITemporaryPower"/>，
	/// 避免临时负面效果与内层正式负面效果重复计数）。
	/// </summary>
	/// <param name="creature">要统计的角色。</param>
	/// <returns>负面效果类型的数量。</returns>
	public static int CountDebuffTypes(Creature creature)
	{
		return creature.Powers
			.Where(IsCountableDebuff)
			.Select(p => p.Id)
			.Distinct()
			.Count();
	}

	/// <summary>
	/// 判定一个 Power 是否计入负面效果类型统计（参考瘟疫 Pestilence.ShouldCountPower）。
	/// </summary>
	private static bool IsCountableDebuff(PowerModel power)
	{
		return power.TypeForCurrentAmount == PowerType.Debuff && power is not ITemporaryPower;
	}

	/// <summary>
	/// 判定一个 Power 当前是否为负面效果。
	/// </summary>
	private static bool IsDebuff(PowerModel power)
	{
		return power.TypeForCurrentAmount == PowerType.Debuff;
	}
}
