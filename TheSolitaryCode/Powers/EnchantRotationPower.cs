using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TheSolitary.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Powers;

// 附魔轮转的 Power（参考无尽刀刃 InfiniteBladesPower 的 BeforeHandDraw 钩子）：
// 每回合开始（抽牌前的 BeforeHandDraw 钩子）交换手牌中两张牌的附魔。
// 交换逻辑复用 EnchantHelpers.SwapEnchantmentsBetweenTwoHandCards（与交换附魔技能共用）。
[RegisterPower]
public sealed class EnchantRotationPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	// 回合开始时触发：手牌不足两张则跳过，否则选两张牌交换附魔。
	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
	{
		if (player != base.Owner.Player)
		{
			return;
		}

		// 手牌不足两张时无法交换，直接跳过（避免弹出选择界面）。
		if (base.Owner.Player.PlayerCombatState!.Hand.Cards.Count < 2)
		{
			return;
		}

		Flash();

		await EnchantHelpers.SwapEnchantmentsBetweenTwoHandCards(
			choiceContext,
			base.Owner.Player,
			new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 2)
			{
				ShouldGlowGold = card => card.Enchantment != null
			},
			this);
	}
}
