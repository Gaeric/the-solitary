using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Powers;

// 黏糊魔典的 Power（战斗结束时触发）：
// 战斗结束时，从拥有者的牌组中选择一张能附魔黏糊（Goopy）的牌附魔。
// 钩子参考禁忌魔典 ForbiddenGrimoirePower 的 AfterCombatEnd 写法；
// 选牌用 FromDeckForEnchantment（自包含同步、无需 PlayerChoiceContext，与 SapphireSeed 事件同款，
// 通过 NOverlayStack 弹出牌组附魔选择覆盖层）。
[RegisterPower]
public sealed class StickyGrimoirePower : ModPowerTemplate
{
	// 黏糊附魔的数值（每打出一次，格挡值永久 +1；与原版 PaelsClaw 遗物用法一致）。
	private const int GoopyAmount = 1;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	// 自定义图标（取材 WatcherBeautified 观者 Power 图标，128x128，小图与大图共用）。
	public override PowerAssetProfile AssetProfile => new(
		IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
		BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png");

	// 悬停提示展示黏糊附魔的效果。
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		HoverTipFactory.FromEnchantment<Goopy>(GoopyAmount);

	// 战斗结束时：从牌组中选择一张能附魔黏糊的牌并附魔（FromDeckForEnchantment 内部已过滤，没有可选牌时不弹界面）。
	public override async Task AfterCombatEnd(CombatRoom room)
	{
		Player player = base.Owner.Player!;
		if (player.Creature.IsDead)
		{
			return;
		}

		Flash();

		CardModel? target = (await CardSelectCmd.FromDeckForEnchantment(
			player: player,
			enchantment: ModelDb.Enchantment<Goopy>().ToMutable(),
			amount: GoopyAmount,
			prefs: new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1))).FirstOrDefault();

		if (target != null)
		{
			CardCmd.Enchant<Goopy>(target, GoopyAmount);
		}
	}
}
