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
// 抽 2 张牌（升级后抽 3 张）；将当前手牌中所有附魔牌的耗能降为 0（本场战斗）。
// “当前手牌”在施放时快照：只有此刻在手牌中的附魔牌享受 0 费，
// 之后才抽到/进入手牌的附魔牌不受影响。
// 实现参考原版 开悟 Enlightenment 的手牌降费机制：升级版 Enlightenment+
// 用 card.EnergyCost.SetThisCombat(1, reduceOnly: true) 对当前手牌统一降费，
// 本卡改为对所有附魔牌 SetThisCombat(0, reduceOnly: true)。
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

	// 基础数值：抽牌数 2（升级后 3），绑定 {Cards:diff()} 占位符。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(2)
	];

	// 打出时：先抽牌，再对当前手牌中所有附魔牌本场战斗降费为 0。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

		// 1. 抽牌（新抽到的附魔牌此时也在手牌中，一并计入“当前手牌”快照）。
		await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);

		// 2. 对当前手牌中所有附魔牌降费为 0（本场战斗永久；reduceOnly 保证只降不升，
		//    参考原版 开悟 Enlightenment+ 的 SetThisCombat）。
		foreach (CardModel card in PileType.Hand.GetPile(Owner).Cards.Where(c => c.Enchantment != null))
		{
			card.EnergyCost.SetThisCombat(0, reduceOnly: true);
		}
	}

	// 升级：抽牌数 2 -> 3。
	protected override void OnUpgrade()
	{
		DynamicVars.Cards.UpgradeValueBy(1);
	}
}
