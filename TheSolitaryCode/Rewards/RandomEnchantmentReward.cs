using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rewards;
using STS2RitsuLib.Combat.Rewards;
using TheSolitary.Cards;
using TheSolitary.Enchantments;

namespace TheSolitary.Rewards;

// 独立奖励类型，避免存档/读档时被误还原为禁忌魔典的删牌奖励。
public sealed class RandomEnchantmentReward : ModCustomReward
{
	private const string LocPrefix = "THE_SOLITARY_CARD_ENCHANT_ASCENSION";
	private static ModRewardDefinition? _definition;

	public static void Register()
	{
		_definition ??= ModRewardRegistry.For(Entry.ModId).RegisterOwned("RandomEnchantment",
			static (_, player, _) => new RandomEnchantmentReward(player));
	}

	public RandomEnchantmentReward(Player player) : base(player) { }

	public override RewardType ModRewardType => (_definition
		?? throw new InvalidOperationException("凝华奖励尚未注册。")).RewardType;
	protected override string DescriptionLocTable => "cards";
	protected override string DescriptionLocKey => $"{LocPrefix}.rewardDescription";
	protected override string RewardIconPath => $"{Entry.ResPath}/images/powers/EnchantAscensionPower.png";
	public override int RewardsSetIndex => 7;

	protected override async Task<bool> OnSelect()
	{
		if (!PileType.Deck.GetPile(Player).Cards.Any(CanEnchant))
		{
			// 无候选时可正常领取并关闭奖励，不留下无法完成的按钮。
			return true;
		}
		var prefs = new CardSelectorPrefs(new LocString("cards", $"{LocPrefix}.selectionScreenPrompt"), 1)
		{
			Cancelable = true,
			RequireManualConfirmation = true
		};
		var card = (await CardSelectCmd.FromDeckGeneric(Player, prefs, CanEnchant)).FirstOrDefault();
		if (card == null)
		{
			return false;
		}
		// 选牌由原版 FromDeckGeneric 同步；所有客户端使用该玩家的奖励 RNG。
		// 直接作用于运行牌组原件，附魔由游戏正常序列化，永久保留。
		RandomEnchantPool.EnchantRandomly(Player.PlayerRng.Rewards, card);
		return true;
	}

	private static bool CanEnchant(MegaCrit.Sts2.Core.Models.CardModel card)
	{
		return card.Enchantment == null && RandomEnchantPool.CanEnchantRandomly(card);
	}

	public override void MarkContentAsSeen() { }
}