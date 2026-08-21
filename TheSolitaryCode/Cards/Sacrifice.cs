using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 献祭（character.org 基础卡 #2）：1 费技能。
// 选择一张手牌消耗。随机为手牌中另一张牌附魔（随机附魔池见 RandomEnchantPool）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Sacrifice : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（设计文档的“基础牌”档位，与符击一致）。
	private const CardRarity CardRarityValue = CardRarity.Common;
	// 目标类型（Self：只作用于己方手牌）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public Sacrifice()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Sacrifice.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 打出时：选择一张手牌消耗，再随机为手牌中另一张牌附魔。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

		// 1. 选择一张手牌消耗。取消选择则整个效果不发（不消耗、不附魔）。
		CardModel? exhausted = (await CardSelectCmd.FromHand(
			prefs: new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1),
			context: choiceContext,
			player: Owner,
			filter: null,
			source: this)).FirstOrDefault();

		if (exhausted == null)
		{
			return;
		}

		await CardCmd.Exhaust(choiceContext, exhausted);

		// 2. 随机为手牌中另一张牌附魔（排除被消耗的牌与已附魔的牌）。
		// OnPlay 必然处于战斗中，PlayerCombatState 一定存在。
		List<CardModel> candidates = Owner.PlayerCombatState!.Hand.Cards
			.Where(card => card != exhausted && card.Enchantment == null)
			.ToList();
		if (candidates.Count == 0)
		{
			return;
		}

		CardModel? target = Owner.RunState.Rng.CombatCardSelection.NextItem(candidates);
		if (target != null)
		{
			RandomEnchantPool.EnchantRandomly(Owner.RunState.Rng.CombatCardSelection, target);
		}
	}

	// 升级：费用 1 -> 0。
	protected override void OnUpgrade()
	{
		base.EnergyCost.UpgradeBy(-1);
	}
}
