using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 观星（character.org todo 金卡）：1 费技能，打出后消耗。
// 依次打出抽牌堆中一张能力牌、技能牌和攻击牌。升级后固有。
// 自动打出参考原版 浩劫 Havoc（先把牌移入打出堆，再 CardCmd.AutoPlay）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class StarGazing : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（金卡 = Rare）。
	private const CardRarity CardRarityValue = CardRarity.Rare;
	// 目标类型（自身：作用于抽牌堆）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public StarGazing()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/StarGazing.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 打出后消耗。
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

	// 打出时：依次从抽牌堆中找出能力牌、技能牌、攻击牌并自动打出。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

		CardPile drawPile = PileType.Draw.GetPile(Owner);
		foreach (CardType type in new[] { CardType.Power, CardType.Skill, CardType.Attack })
		{
			// 从抽牌堆顶部找一张对应类型的牌（找不到则跳过该类型）。
			CardModel? card = drawPile.Cards.FirstOrDefault(c => c.Type == type);
			if (card == null)
			{
				continue;
			}

			// 先移入打出堆再自动打出（与浩劫 Havoc 的 AutoPlayFromDrawPile 一致；
			// 目标类型为任意敌人的牌由 AutoPlay 随机选取目标）。
			await CardPileCmd.Add(card, PileType.Play);
			await CardCmd.AutoPlay(choiceContext, card, null);
		}
	}

	// 升级：获得固有。
	protected override void OnUpgrade()
	{
		AddKeyword(CardKeyword.Innate);
	}
}
