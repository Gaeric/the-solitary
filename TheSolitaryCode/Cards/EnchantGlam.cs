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

// 光谱 Spectrum（原附魔华彩，character.org 金卡 #2）：0 费技能，保留、消耗。
// 获得 10 点格挡；选择抽牌堆中两张牌附魔华彩（Glam：每场战斗第一次打出时额外打出一次）。升级后固有。
// 选牌附魔套路参考魂回之环 RingOfSoulReturn（FromCombatPile 从抽牌堆选牌）＋ 余烬庇护 EmberShelter（悬停提示）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class EnchantGlam : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 0;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（金卡 = Rare）。
	private const CardRarity CardRarityValue = CardRarity.Rare;
	// 目标类型（Self：作用于己方抽牌堆）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public EnchantGlam()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 获得格挡，按格挡牌参与力量/敏捷等计算。
	public override bool GainsBlock => true;

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/EnchantGlam.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：格挡 10（绑定 {Block:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(10m, ValueProp.Move)
	];

	// 保留关键字（本回合未打出的情况下保留在手牌）；打出后消耗（升级前后均消耗）。
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];

	// 悬停提示：华彩附魔（Glam：每场战斗第一次打出时额外打出一次）。
	// 注意：HoverTipFactory.FromEnchantment<T>() 本身返回 IEnumerable<IHoverTip>，不能再用集合表达式包一层。
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		HoverTipFactory.FromEnchantment<Glam>();

	// 打出时：先获得格挡，再从抽牌堆选择两张能附魔华彩的牌并附魔。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

		// 1. 获得 10 点格挡。
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

		// 2. 从抽牌堆选择两张能附魔华彩的牌。filter 与 CardCmd.Enchant 内部检查一致，
		//    避免 CanEnchant 失败抛异常。FromCombatPile 自动处理数量：
		//    0 张可用 -> 跳过；1 张 -> 自动附魔；2 张 -> 全部附魔；3 张以上 -> 弹选择界面选恰好 2 张。
		List<CardModel> selection = (await CardSelectCmd.FromCombatPile(
			prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 2),
			context: choiceContext,
			pile: PileType.Draw.GetPile(Owner),
			player: Owner,
			filter: CanEnchantGlam)).ToList();

		foreach (CardModel card in selection)
		{
			CardCmd.Enchant<Glam>(card, 1m);
		}
	}

	// 升级：获得固有（战斗开始时在手牌中）。
	protected override void OnUpgrade()
	{
		AddKeyword(CardKeyword.Innate);
	}

	// 与 CardCmd.Enchant 内部的 CanEnchant 检查一致：只允许选择能附魔华彩的牌。
	private static bool CanEnchantGlam(CardModel card)
	{
		return ModelDb.Enchantment<Glam>().ToMutable().CanEnchant(card);
	}
}
