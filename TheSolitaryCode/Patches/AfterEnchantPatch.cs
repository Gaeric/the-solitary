using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Powers;
using STS2RitsuLib.Patching.Models;

namespace TheSolitary.Patches;

// "获得/生成附魔后"的统一触发补丁。
// 游戏没有任何"获得附魔后"的事件钩子（CardCmd.Enchant 不触发任何 Hook），
// 因此在 CardCmd.Enchant（所有附魔动作的唯一入口）成功后统一派发：
//   - EnchantResonancePower（附魔共鸣）：获得附魔时获得等量活力（VigorPower）；
//   - ReverbPower（余音）：生成附魔时获得等量格挡。
// 以后新增"附魔后触发"的 Power 时，只需在此 Postfix 追加一个分支。
public sealed class AfterEnchantPatch : IPatchMethod
{
	// 补丁 ID（RitsuLib 要求全局唯一）。
	public static string PatchId => "thesolitary_after_enchant";

	public static string Description =>
		"Grant Vigor / Block whenever a card gains an Enchantment while Enchant Resonance / Reverb is active";

	// 非关键补丁：若游戏更新导致 CardCmd.Enchant 签名变化，仅本功能失效，不影响整个 Mod。
	public static bool IsCritical => false;

	public static ModPatchTarget[] GetTargets() =>
	[
		new ModPatchTarget(
			typeof(CardCmd),
			nameof(CardCmd.Enchant),
			[typeof(EnchantmentModel), typeof(CardModel), typeof(decimal)],
			ignoreIfMissing: true)
	];

	// 记录附魔前该卡是否已带有附魔，用于区分"新获得附魔"与"同类型附魔叠层"。
	public static void Prefix(CardModel card, out bool __state)
	{
		__state = card.Enchantment != null;
	}

	// CardCmd.Enchant 成功后触发（__result 为施加成功的附魔）。
	// 注意：PowerCmd.Apply / CreatureCmd.GainBlock 内部会走 Godot 计时器 / Flash 等，
	// 必须回到主线程续延；Godot 4 已安装 GodotSynchronizationContext，async 续延会回到主线程。
	public static async void Postfix(CardModel card, bool __state, EnchantmentModel? __result)
	{
		try
		{
			// 附魔失败、或只是给已附魔的牌叠同类型层数（Amount 增加），都不算"获得/生成附魔"。
			if (__result == null || __state || card.Owner?.Creature is not { } creature)
			{
				return;
			}

			// 附魔共鸣：获得附魔时获得活力。
			var resonance = creature.GetPower<EnchantResonancePower>();
			if (resonance != null)
			{
				// 活力施加不会触发玩家选择，用 ThrowingPlayerChoiceContext（与原版 PrepTimePower 一致）。
				await PowerCmd.Apply<VigorPower>(
					new ThrowingPlayerChoiceContext(), creature, resonance.Amount, creature, null);
			}

			// 余音：生成附魔时获得格挡（ValueProp.Unpowered：来自 Power 的格挡，不享受力量/敏捷）。
			var ward = creature.GetPower<ReverbPower>();
			if (ward != null)
			{
				await CreatureCmd.GainBlock(creature, ward.Amount, ValueProp.Unpowered, null);
			}
		}
		catch (Exception ex)
		{
			// 异步 void 里的异常必须捕获，否则会直接崩溃游戏。
			Entry.Logger.Error(ex.ToString());
		}
	}
}
