using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 重采样（character.org 金卡 #5）：0 费技能。
// 抽 2 张牌（升级后抽 3 张）；你手牌中的所有附魔牌在本回合免费打出。
// 实现参考原版 子弹时间 BulletTime：用卡牌级 SetToFreeThisTurn()
// 同时归零能量费与星费，并跳过 X 费用牌。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Resampling : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 0;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（金卡 = Rare）。
	private const CardRarity CardRarityValue = CardRarity.Rare;
	// 目标类型（自身）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public Resampling()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Resampling.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 关键词：消耗（升级前后均保留；自动显示在卡面，无需写进描述）。
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

	// 基础数值：抽牌数 2（升级后 3），绑定 {Cards:diff()} 占位符。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(2)
	];

	// 打出时：先抽牌，再让当前手牌中所有附魔牌在本回合免费打出。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

		// 1. 抽牌（新抽到的附魔牌此时也在手牌中，一并享受本回合免费打出）。
		await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);

		// 2. 让当前手牌中所有附魔牌在本回合免费打出。
		//    参考原版 子弹时间 BulletTime：用卡牌级 SetToFreeThisTurn()
		//    （能量费与星费都归零），并跳过 X 费用牌避免破坏其 X 机制。
		foreach (CardModel card in PileType.Hand.GetPile(Owner).Cards.Where(c => c.Enchantment != null))
		{
			if (!card.EnergyCost.CostsX)
			{
				card.SetToFreeThisTurn();
			}
		}
	}

	// 升级：抽牌数 2 -> 3。
	protected override void OnUpgrade()
	{
		DynamicVars.Cards.UpgradeValueBy(1);
	}
}
