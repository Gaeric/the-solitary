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

// 魂回之环（character.org 蓝卡 #5）：0 费技能，消耗。
// 获得 7 点格挡；选择抽牌堆中一张牌附魔灵魂之力（SoulsPower：该牌失去消耗）。升级后固有。
// 实现参考原版 冲锋 Charge（CardSelectCmd.FromCombatPile 从抽牌堆选牌）＋
// 稳定召回 StableRecall（获得格挡 + 从战斗牌堆选牌附魔 + 消耗 + 附魔悬停提示）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class RingOfSoulReturn : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 0;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（蓝卡 = Uncommon）。
	private const CardRarity CardRarityValue = CardRarity.Uncommon;
	// 目标类型（Self：作用于己方抽牌堆）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public RingOfSoulReturn()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 获得格挡，按格挡牌参与力量/敏捷等计算。
	public override bool GainsBlock => true;

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/RingOfSoulReturn.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：格挡 7（绑定 {Block:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(7m, ValueProp.Move)
	];

	// 打出后自动消耗。
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

	// 悬停提示：灵魂之力附魔（SoulsPower：该牌失去消耗）。
	// 注意：HoverTipFactory.FromEnchantment<T>() 本身返回 IEnumerable<IHoverTip>，不能再用集合表达式包一层。
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		HoverTipFactory.FromEnchantment<SoulsPower>();

	// 打出时：先获得格挡，再从抽牌堆选一张能附魔灵魂之力的牌（需带消耗）并附魔。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

		// 1. 获得 7 点格挡。
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

		// 2. 抽牌堆中没有能被灵魂之力附魔的牌（需带消耗的牌）时，直接跳过（不弹选择界面）。
		if (!PileType.Draw.GetPile(Owner).Cards.Any(CanEnchantSoulsPower))
		{
			return;
		}

		// 3. 从抽牌堆选择一张牌附魔灵魂之力。filter 只放行能附魔的牌（需带消耗，
		//    状态/诅咒等类型会被 CanEnchant 拒绝，已附魔的牌也会被排除），
		//    避免 CardCmd.Enchant 因 CanEnchant 失败而抛异常。
		CardModel? picked = (await CardSelectCmd.FromCombatPile(
			prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 1),
			context: choiceContext,
			pile: PileType.Draw.GetPile(Owner),
			player: Owner,
			filter: CanEnchantSoulsPower)).FirstOrDefault();

		if (picked != null)
		{
			CardCmd.Enchant<SoulsPower>(picked, 1m);
		}
	}

	// 升级：获得固有（参考能元妙术 ArtOfTheSource 的升级方式）。
	protected override void OnUpgrade()
	{
		AddKeyword(CardKeyword.Innate);
	}

	// 与 CardCmd.Enchant 内部的 CanEnchant 检查一致：只允许选择能附魔灵魂之力的牌。
	private static bool CanEnchantSoulsPower(CardModel card)
	{
		return ModelDb.Enchantment<SoulsPower>().ToMutable().CanEnchant(card);
	}
}
