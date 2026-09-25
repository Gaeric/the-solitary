using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 融会贯通（character.org 稀有卡）：1 费技能，无其它关键词（升级后 0 费）。
// 随机交换手牌中所有非攻击牌的附魔：取出手牌里"带附魔且不是攻击牌"的牌组成列表，
// 在这份列表内部把附魔随机重新分配——每张牌都必定拿到"别人原来的附魔"，
// 绝不会出现某张牌保留自己附魔的情况（随机错排）。
// 列表里恰好只有一张附魔牌时改为把这唯一一张牌的附魔"重新充能"（刷新一次性状态），
// 没有附魔牌时无事发生，详细规则见 EnchantHelpers.ShuffleEnchantmentsInCards。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Mastery : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（金卡 = Rare）。
	private const CardRarity CardRarityValue = CardRarity.Rare;
	// 目标类型（自身：作用于手牌）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public Mastery()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Mastery.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 打出时：把"手牌中所有带附魔的非攻击牌"的附魔在它们之间随机重排。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

		// 候选列表：手牌中带附魔、且不是攻击牌的牌
		// （没有附魔的牌没有可交换的对象；攻击牌按设计排除在外）。
		List<CardModel> targets = Owner.PlayerCombatState!.Hand.Cards
			.Where(card => card.Type != CardType.Attack && card.Enchantment != null)
			.ToList();

		// 在列表内部随机重新分配附魔
		// （只有一张时改为把它的附魔重新充能、没有附魔牌时无事发生，均在方法内处理）。
		// 随机源与其它"随机挑一张手牌"的效果一致（参考唤醒/涟漪/复苏 用 CombatCardSelection）。
		EnchantHelpers.ShuffleEnchantmentsInCards(targets, Owner.RunState.Rng.CombatCardSelection);
	}

	// 升级：耗能 1 -> 0。
	protected override void OnUpgrade()
	{
		base.EnergyCost.UpgradeBy(-1);
	}
}
