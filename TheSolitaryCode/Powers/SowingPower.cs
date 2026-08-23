using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Powers;

// 播种的 Power（参考计策 StratagemPower 的 AfterShuffle 钩子）：
// 当拥有者的抽牌堆被打乱洗牌时（前 Amount 次，即前 2 次），从抽牌堆（牌组）中选择一张牌附魔播种
// （Sown：每场战斗第一次打出时获得 Amount 点能量）。Amount 即剩余触发次数，每次触发 -1。
[RegisterPower]
public sealed class SowingPower : ModPowerTemplate
{
	// 播种附魔的数值（每场战斗第一次打出时获得的能量；与原版 SapphireSeed 事件的用法一致）。
	private const int SownAmount = 1;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	// 抽牌堆被洗牌时触发（与 StratagemPower 相同的钩子：先校验洗牌者，再 Flash + 生效）。
	public override async Task AfterShuffle(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != base.Owner.Player)
		{
			return;
		}

		// 前 2 次洗牌之后不再触发。
		if (base.Amount <= 0)
		{
			return;
		}

		Flash();

		// 从抽牌堆（牌组）中选择一张可附魔播种的牌；时机与方法参考计策（Stratagem）：
		// 触发时机 = 抽牌堆被打乱洗牌（AfterShuffle 钩子）；选择来源 = 抽牌堆 FromCombatPile(Draw)。
		// 没有可选牌时 FromCombatPile 返回空，不会弹选择界面。
		CardModel? target = (await CardSelectCmd.FromCombatPile(
			context: choiceContext,
			pile: PileType.Draw.GetPile(base.Owner.Player),
			player: base.Owner.Player,
			prefs: new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1),
			filter: CanEnchantSown)).FirstOrDefault();

		if (target != null)
		{
			CardCmd.Enchant<Sown>(target, SownAmount);
		}

		// 消耗一次洗牌触发次数（即使没有可附魔的牌，本次洗牌机会也已使用）。
		base.SetAmount(base.Amount - 1);
	}

	// 与 CardCmd.Enchant 内部的 CanEnchant 检查一致：只允许选择能附魔播种的牌。
	private static bool CanEnchantSown(CardModel card)
	{
		return ModelDb.Enchantment<Sown>().ToMutable().CanEnchant(card);
	}
}
