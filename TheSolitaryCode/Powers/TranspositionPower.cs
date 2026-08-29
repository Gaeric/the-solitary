using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TheSolitary.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Powers;

// 元能转置的 Power（AfterPlayerTurnStart 回合开始钩子）：
// 每回合开始抽牌后（AfterPlayerTurnStart）交换手牌中两张牌的附魔。
// 注意不能用 BeforeHandDraw（抽牌前手牌为空），否则“手牌不足两张”会永远成立、效果从不触发。
// 交换逻辑复用 EnchantHelpers.SwapEnchantmentsBetweenTwoHandCards（与轮回技能共用）。
[RegisterPower]
public sealed class TranspositionPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	// 自定义图标（取材 WatcherBeautified 观者 Power 图标，128x128，小图与大图共用）。
	public override PowerAssetProfile AssetProfile => new(
		IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
		BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png");

	// 回合开始时触发（抽牌后，手牌已就绪）：手牌不足两张则跳过，否则选两张牌交换附魔。
	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
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
			new CardSelectorPrefs(base.SelectionScreenPrompt, 2)
			{
				ShouldGlowGold = card => card.Enchantment != null
			},
			this);
	}
}
