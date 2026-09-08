using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 伪影（character.org todo 蓝卡）：1 费技能，打出后消耗。
// 抽 2 张牌（升级后抽 3 张）；选择 2 张手牌中的攻击牌附魔墨影（Inky：打出时施加 1 层虚弱）。
// 墨影只限定攻击牌：参考原版 Instinct/Sharp/Momentum 用 CanEnchantCardType 限定 CardType.Attack 的做法，
// 避免把墨影附到自目标/技能牌上时 Inky.OnPlay 对 cardPlay.Target（即自己）施加虚弱。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Artifact : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（蓝卡 = Uncommon）。
	private const CardRarity CardRarityValue = CardRarity.Uncommon;
	// 目标类型（自身：作用于手牌）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public Artifact()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Artifact.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 打出后消耗。
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

	// 基础数值：抽牌数（绑定 {Cards:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(2)
	];

	// 打出时：抽牌，再选择 2 张手牌附魔墨影。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

		// 1. 抽牌。
		await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);

		// 2. 手牌中没有能被墨影附魔的攻击牌时，跳过附魔（不弹选择界面）。
		if (!Owner.PlayerCombatState!.Hand.Cards.Any(CanEnchantInky))
		{
			return;
		}

		// 3. 选择 2 张手牌中的攻击牌附魔墨影（filter 只放行攻击牌且能附魔的牌）。
		List<CardModel> selected = (await CardSelectCmd.FromHand(
			context: choiceContext,
			player: Owner,
			prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 2),
			filter: CanEnchantInky,
			source: this)).ToList();

		// 取消选择（不足 2 张）则只附魔选中的部分。
		foreach (CardModel card in selected)
		{
			CardCmd.Enchant<Inky>(card, 1m);
		}
	}

	// 升级：抽 2 -> 3 张牌。
	protected override void OnUpgrade()
	{
		DynamicVars.Cards.UpgradeValueBy(1);
	}

	/// <summary>
	/// 检查目标牌能否附魔墨影（Inky）：墨影只允许附魔攻击牌，
	/// 并在攻击牌基础上再走一遍 CardCmd.Enchant 内部的 CanEnchant 通用检查。
	/// </summary>
	private static bool CanEnchantInky(CardModel card)
	{
		return card.Type == CardType.Attack && ModelDb.Enchantment<Inky>().ToMutable().CanEnchant(card);
	}
}
