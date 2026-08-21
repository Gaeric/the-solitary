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

// 熔炼（character.org 白卡 #16）：1 费技能。
// 选择一张手牌消耗。获得 8 点格挡；若该牌有附魔，额外获得 8 点格挡（升级后基础格挡 +3）。
// 选择交互参考献祭 Sacrifice（CardSelectCmd.FromHand + ExhaustSelectionPrompt）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Smelt : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（白卡 = Common）。
	private const CardRarity CardRarityValue = CardRarity.Common;
	// 目标类型（Self：只作用于己方手牌）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;
	// 附魔额外格挡的 DynamicVar 键名（绑定 {EnchantedBonus:diff()} 占位符）。
	private const string EnchantedBonusKey = "EnchantedBonus";
	// 附魔牌的额外格挡。
	private const int EnchantedBonus = 8;

	public Smelt()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 获得格挡，按格挡牌参与力量/敏捷等计算。
	public override bool GainsBlock => true;

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Smelt.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：基础格挡 8 + 附魔额外格挡 8（绑定 {Block:diff()} / {EnchantedBonus:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(8m, ValueProp.Move),
		new DynamicVar(EnchantedBonusKey, EnchantedBonus)
	];

	// 打出时：选择一张手牌消耗；获得格挡；若该牌带附魔则额外获得格挡。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

		// 1. 选择一张手牌消耗。取消选择则整个效果不发（不消耗、不获得格挡）。
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

		// 先记录该牌是否带附魔，再消耗。
		bool wasEnchanted = exhausted.Enchantment != null;

		await CardCmd.Exhaust(choiceContext, exhausted);

		// 2. 获得基础格挡。
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

		// 3. 若被消耗的牌带附魔，额外获得格挡。
		if (wasEnchanted)
		{
			await CreatureCmd.GainBlock(Owner.Creature, DynamicVars[EnchantedBonusKey].BaseValue, ValueProp.Move, cardPlay);
		}
	}

	// 升级：基础格挡 8 -> 11。
	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(3m);
	}
}
