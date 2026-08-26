using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 融会贯通（character.org 金卡 #11）：2 费技能，固有、消耗，升级后 1 费。
// 打击和防御附魔涡旋（Spiral：获得重放 1），并获得消耗。
// 效果仅限本场战斗生效：遍历当前战斗牌堆中的牌附魔涡旋 + 给予消耗，
// 不同步牌组原件（DeckVersion），因此跨战斗不保留，下一场战斗恢复原状。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Mastery : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 2;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（金卡 = Rare）。
	private const CardRarity CardRarityValue = CardRarity.Rare;
	// 目标类型（Self：作用于己方整个牌组）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public Mastery()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 固有：每场战斗开始时进入起手；消耗：打出后移除。
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Innate, CardKeyword.Exhaust];

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Mastery.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 悬停提示：涡旋附魔（Spiral：该牌获得重放 1）。
	// 注意：HoverTipFactory.FromEnchantment<T>() 本身返回 IEnumerable<IHoverTip>，不能再用集合表达式包一层。
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		HoverTipFactory.FromEnchantment<Spiral>();

	// 打出时：遍历所有战斗牌堆，为每张能被涡旋附魔的打击/防御基础牌
	// 附魔涡旋并给予消耗；仅影响本场战斗，不写回牌组原件（跨战斗不保留）。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

		foreach (CardModel card in EnchantHelpers.GetAllCombatPileCards(Owner))
		{
			if (!CanEnchantSpiral(card))
			{
				continue;
			}

			// 当前战斗副本立即生效（手牌/抽牌堆等可见牌会即时刷新卡面）。
			CardCmd.Enchant<Spiral>(card, 1m);
			CardCmd.ApplyKeyword(card, CardKeyword.Exhaust);
		}
	}

	// 升级：费用 2 -> 1。
	protected override void OnUpgrade()
	{
		base.EnergyCost.UpgradeBy(-1);
	}

	// 与 CardCmd.Enchant 内部的 CanEnchant 检查一致：涡旋原版限定只能附魔基础打击/防御
	// （即已更名的初始打击 Strike / 初始防御 Defend；通过 CardTag.Strike/Defend + Basic 稀有度匹配，与类名无关）。
	private static bool CanEnchantSpiral(CardModel card)
	{
		return ModelDb.Enchantment<Spiral>().ToMutable().CanEnchant(card);
	}
}
