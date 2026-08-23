using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 唤醒（character.org 基础卡 #2）：1 费技能。
// 获得 8 点格挡；选择一张手牌消耗，随机为手牌中另一张牌附魔（随机附魔池见 RandomEnchantPool）。
[RegisterCard(typeof(TheSolitaryCardPool))]
[RegisterCharacterStarterCard(typeof(TheSolitaryCharacter), 1, Order = 1)]
public sealed class Sacrifice : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（基础牌 = 初始卡，不出现在奖励/商店列表中；与匣中术一致）。
	private const CardRarity CardRarityValue = CardRarity.Basic;
	// 目标类型（Self：只作用于己方手牌）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public Sacrifice()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 获得格挡，按格挡牌参与力量/敏捷等计算。
	public override bool GainsBlock => true;

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Sacrifice.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：格挡 8（升级后 11），绑定 {Block:diff()} 占位符。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(8m, ValueProp.Move)
	];

	// 打出时：获得格挡，选择一张手牌消耗，再随机为手牌中另一张牌附魔。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

		// 1. 获得格挡。
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

		// 2. 选择一张手牌消耗。取消选择则跳过消耗/附魔部分（格挡已获得）。
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

		// 3. 随机为手牌中另一张牌附魔（排除被消耗的牌与已附魔的牌）。
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

	// 升级：格挡 8 -> 11。
	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(3m);
	}
}
