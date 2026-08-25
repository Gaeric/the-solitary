using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 余烬庇护（新卡）：1 费技能。
// 获得 8 点格挡；选择一张手牌附魔特兹卡塔拉的余烬
// （TezcatarasEmber：费用为 0、造成 3 点额外伤害、且获得永恒 Eternal）。
// 实现参考稳定召回 StableRecall（获得格挡 + 从牌堆选牌附魔）的套路；
// 附魔施加方式参考原版遗物 NutritiousSoup（CardCmd.Enchant<TezcatarasEmber>(card, 1m)）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class EmberShelter : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（蓝卡 = Uncommon）。
	private const CardRarity CardRarityValue = CardRarity.Uncommon;
	// 目标类型（Self：作用于己方手牌）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	// 消耗：升级前后都消耗（关键字作用于基础卡，升级不会移除）。
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

	public EmberShelter()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 获得格挡，按格挡牌参与力量/敏捷等计算。
	public override bool GainsBlock => true;

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/EmberShelter.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：格挡 8（升级后 11），绑定 {Block:diff()} 占位符。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(8m, ValueProp.Move)
	];

	// 悬停提示：特兹卡塔拉的余烬附魔（费用 0 / +3 伤害 / 永恒）。
	// 注意：HoverTipFactory.FromEnchantment<T>() 本身返回 IEnumerable<IHoverTip>，不能再用集合表达式包一层。
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		HoverTipFactory.FromEnchantment<TezcatarasEmber>();

	// 打出时：先获得格挡，再从手牌选一张能附魔余烬的牌并附魔。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

		// 1. 获得格挡。
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

		// 2. 选择一张能附魔余烬的手牌。filter 只放行能附魔的牌（状态/诅咒等类型会被
		//    CanEnchant 拒绝，已附魔的牌也会被排除），避免 CardCmd.Enchant 因 CanEnchant 失败而抛异常。
		CardModel? target = (await CardSelectCmd.FromHand(
			context: choiceContext,
			player: Owner,
			prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 1),
			filter: CanEnchantEmber,
			source: this)).FirstOrDefault();

		// 手牌中没有可附魔的牌时跳过附魔（格挡已获得，不弹选择界面）。
		if (target != null)
		{
			CardCmd.Enchant<TezcatarasEmber>(target, 1m);
		}
	}

	// 升级：格挡 8 -> 11。
	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(3m);
	}

	// 与 CardCmd.Enchant 内部的 CanEnchant 检查一致：只允许选择能附魔余烬的牌。
	private static bool CanEnchantEmber(CardModel card)
	{
		return ModelDb.Enchantment<TezcatarasEmber>().ToMutable().CanEnchant(card);
	}
}
