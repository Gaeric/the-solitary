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

// 深度检测（character.org todo 白卡）：1 费技能，打出后消耗。
// 获得 3 点格挡（升级后 5 点）；将弃牌堆中一张牌放入手牌；
// 如果那张牌带有附魔，获得 1 点能量。
// 弃牌堆回收参考召回 StableRecall（CardSelectCmd.FromCombatPile + CardPileCmd.Add 到手牌）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class DeepDetection : ModCardTemplate
{
	private const int BaseEnergyCost = 1;
	private const CardType CardKind = CardType.Skill;
	private const CardRarity CardRarityValue = CardRarity.Common;
	private const TargetType CardTarget = TargetType.Self;
	private const bool ShowInCardLibrary = true;

	public DeepDetection()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	public override bool GainsBlock => true;

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

	// 基础数值：格挡 + 回收附魔牌时获得的能量（绑定 {Block:diff()} / {Energy:energyIcons()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(3m, ValueProp.Move),
		new EnergyVar(1)
	];

	// 打出时：获得格挡，从弃牌堆选一张牌放入手牌；回收的牌带附魔则获得能量。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

		// 1. 从弃牌堆选择一张牌（无附魔限制，任意牌皆可回收）。
		CardModel? picked = (await CardSelectCmd.FromCombatPile(
			prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 1),
			context: choiceContext,
			pile: PileType.Discard.GetPile(Owner),
			player: Owner,
			filter: null)).FirstOrDefault();

		// 弃牌堆为空（或取消选择）时跳过回收。
		if (picked == null)
		{
			return;
		}

		// 2. 放入手牌。
		await CardPileCmd.Add(picked, PileType.Hand);

		// 3. 回收的牌带有附魔时，获得能量。
		if (picked.Enchantment != null)
		{
			await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
		}
	}

	// 升级：格挡 3 -> 5。
	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(2m);
	}
}
