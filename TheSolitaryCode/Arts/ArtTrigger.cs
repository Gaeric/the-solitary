using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Powers;

namespace TheSolitary.ArtEffects;

// 归属只用于选敌和后续术式专用联动，不作为伤害的 dealer / cardSource / cardPlay。
public sealed record ArtTriggerResult(Player Owner, ArtKind Kind, bool Upgraded, Creature Target);

public static class ArtTrigger
{
	private static readonly ArtKind[] All = Enum.GetValues<ArtKind>();

	/// <summary>
	/// 随机触发一次术式。无存活可命中敌人时不结算，返回 null。
	/// 保留原有随机池规则：所有可命中敌人均已有缓慢时，不再选择凋零。
	/// </summary>
	public static async Task<ArtTriggerResult?> TriggerRandom(
		Player owner, ICombatState combatState, Rng rng, PlayerChoiceContext choiceContext,
		bool upgraded = false, Creature? target = null)
	{
		List<Creature> enemies = GetTargets(owner, combatState);
		if (CombatManager.Instance.IsOverOrEnding || enemies.Count == 0)
		{
			return null;
		}
		ValidateTarget(target, enemies);

		IEnumerable<ArtKind> candidates = enemies.Any(e => !e.HasPower<SlowPower>())
			? All
			: All.Where(kind => kind != ArtKind.Decay);
		ArtKind kind = rng.NextItem(candidates);
		target ??= owner.RunState.Rng.CombatTargets.NextItem(enemies)!;
		return await Trigger(owner, combatState, kind, choiceContext, upgraded, target);
	}

	/// <summary>
	/// 触发指定术式。默认随机选敌，也允许调用方明确指定一个可命中敌人。
	/// 不创建卡牌、不进入牌堆、不触发生成牌/打牌/消耗/充能球事件。
	/// </summary>
	public static async Task<ArtTriggerResult?> Trigger(
		Player owner, ICombatState combatState, ArtKind kind, PlayerChoiceContext choiceContext,
		bool upgraded = false, Creature? target = null)
	{
		// 一次来源触发只读取一次回想层数，额外结算不再经过此入口，避免递归。
		int extraTriggers = Math.Max(0, (int)owner.Creature.GetPowerAmount<SpellRecallPower>());
		if (extraTriggers > 0 && !CombatManager.Instance.IsOverOrEnding && GetTargets(owner, combatState).Count > 0)
		{
			owner.Creature.GetPower<SpellRecallPower>()?.Flash();
		}
		ArtTriggerResult? first = null;
		for (int i = 0; i <= extraTriggers; i++)
		{
			// 首次目标严格校验；重复效果在原目标死亡后改为随机敌人。
			if (i > 0 && target != null && !target.IsHittable)
			{
				target = null;
			}
			ArtTriggerResult? result = await TriggerOnce(owner, combatState, kind, choiceContext, upgraded, target);
			if (result == null)
			{
				break;
			}
			first ??= result;
			target = result.Target;
		}
		return first;
	}

	private static async Task<ArtTriggerResult?> TriggerOnce(
		Player owner, ICombatState combatState, ArtKind kind, PlayerChoiceContext choiceContext,
		bool upgraded, Creature? target)
	{
		ArtDefinition effect = ArtDefinition.Get(kind, upgraded);
		List<Creature> enemies = GetTargets(owner, combatState);
		if (CombatManager.Instance.IsOverOrEnding || enemies.Count == 0)
		{
			return null;
		}
		ValidateTarget(target, enemies);
		target ??= owner.RunState.Rng.CombatTargets.NextItem(enemies)!;
		// 与原版 AfterCardPlayed 相同，先快照监听者；致死的这次术式也须完成通知。
		var listeners = combatState.IterateHookListeners()
			.Where(model => model is IArtTriggerListener).ToList();
		await PowerCmd.Apply<ArtTrackerPower>(choiceContext, owner.Creature, 1m, null, null, silent: true);

		ArtTriggerVfx.Play(target, effect);

		// Unpowered 且三种来源全部为空：不走攻击伤害，不继承来源卡的附魔或重放。
		await CreatureCmd.Damage(choiceContext, target, effect.Damage, ValueProp.Unpowered,
			dealer: null, cardSource: null, cardPlay: null);

		// 与旧术式一致，先伤害再挂负面；致死或战斗已结束时不再向目标施加 Power。
		if (target.IsHittable && !CombatManager.Instance.IsOverOrEnding)
		{
			switch (kind)
			{
				case ArtKind.Decay:
					await PowerCmd.Apply<SlowPower>(choiceContext, target, effect.PowerAmount, null, null);
					break;
				case ArtKind.Wilt:
					await PowerCmd.Apply<WeakPower>(choiceContext, target, effect.PowerAmount, null, null);
					break;
				case ArtKind.Breach:
					await PowerCmd.Apply<VulnerablePower>(choiceContext, target, effect.PowerAmount, null, null);
					break;
				case ArtKind.Venom:
					await PowerCmd.Apply<PoisonPower>(choiceContext, target, effect.PowerAmount, null, null);
					break;
				case ArtKind.Doom:
					await PowerCmd.Apply<DoomPower>(choiceContext, target, effect.PowerAmount, null, null);
					break;
			}
		}

		var result = new ArtTriggerResult(owner, kind, upgraded, target);
		foreach (var model in listeners)
		{
			choiceContext.PushModel(model);
			try
			{
				await ((IArtTriggerListener)model).AfterArtTriggered(choiceContext, result);
				model.InvokeExecutionFinished();
			}
			finally
			{
				choiceContext.PopModel(model);
			}
		}
		return result;
	}

	private static List<Creature> GetTargets(Player owner, ICombatState combatState)
	{
		return combatState.GetOpponentsOf(owner.Creature).Where(e => e.IsHittable).ToList();
	}

	private static void ValidateTarget(Creature? target, List<Creature> enemies)
	{
		if (target != null && !enemies.Contains(target))
		{
			throw new ArgumentException("术式只能指定本场战斗中可命中的敌人。", nameof(target));
		}
	}
}