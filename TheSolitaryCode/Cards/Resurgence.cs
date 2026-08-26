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

// 复苏（先古卡，唤醒 Sacrifice 的先古升级版）：1 费技能，升级后 0 费。
// 获得 16 点格挡；选择 2 张手牌消耗，随机为另外 2 张手牌附魔。
// 与基础牌唤醒的关系参考原版 中和 Neutralize -> 压制 Suppress：
// 同机制、数值大幅放大、独立成卡（Ancient 稀有度）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Resurgence : ModCardTemplate
{
	// 选择消耗的手牌数 / 随机附魔的卡牌数。
	private const int ExhaustCount = 2;
	private const int EnchantCount = 2;

	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（先古 = Ancient）。
	private const CardRarity CardRarityValue = CardRarity.Ancient;
	// 目标类型（Self：只作用于己方手牌）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public Resurgence()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 获得格挡，按格挡牌参与力量/敏捷等计算。
	public override bool GainsBlock => true;

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Resurgence.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：格挡 16，绑定 {Block:diff()} 占位符。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(16m, ValueProp.Move)
	];

	// 打出时：获得格挡，选择 2 张手牌消耗，再随机为另外 2 张手牌附魔。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

		// 1. 获得格挡。
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

		// 2. 选择 2 张手牌消耗。取消选择则跳过消耗/附魔部分（格挡已获得，与唤醒一致）。
		List<CardModel> exhausted = (await CardSelectCmd.FromHand(
			prefs: new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, ExhaustCount),
			context: choiceContext,
			player: Owner,
			filter: null,
			source: this)).ToList();

		if (exhausted.Count < ExhaustCount)
		{
			return;
		}

		foreach (CardModel card in exhausted)
		{
			await CardCmd.Exhaust(choiceContext, card);
		}

		// 3. 随机为手牌中另外 2 张牌附魔（排除被消耗的牌、已附魔的牌与无法附魔的牌）。
		// OnPlay 必然处于战斗中，PlayerCombatState 一定存在。
		List<CardModel> candidates = Owner.PlayerCombatState!.Hand.Cards
			.Where(card => !exhausted.Contains(card) && card.Enchantment == null && RandomEnchantPool.CanEnchantRandomly(card))
			.ToList();

		for (int i = 0; i < EnchantCount && candidates.Count > 0; i++)
		{
			CardModel? target = Owner.RunState.Rng.CombatCardSelection.NextItem(candidates);
			if (target == null)
			{
				break;
			}
			candidates.Remove(target);
			RandomEnchantPool.EnchantRandomly(Owner.RunState.Rng.CombatCardSelection, target);
		}
	}

	// 升级：费用 1 -> 0（格挡与消耗/附魔数量不变）。
	protected override void OnUpgrade()
	{
		base.EnergyCost.UpgradeBy(-1);
	}
}
