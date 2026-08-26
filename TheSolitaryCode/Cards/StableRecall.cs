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

// 召回（原稳定召回，character.org 蓝卡 #7）：1 费技能。
// 获得 5 点格挡；从弃牌堆选择一张牌加入手牌，并附魔稳定（Steady：该牌获得保留）。消耗。
// 实现参考原版全息影像 Hologram（从弃牌堆回收一张牌）＋ 稳定附魔 Steady
// （附魔机制，施加方式参考沉没抄本 WaterloggedScriptorium 的 CardCmd.Enchant<Steady>）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class StableRecall : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（蓝卡 = Uncommon）。
	private const CardRarity CardRarityValue = CardRarity.Uncommon;
	// 目标类型（Self：作用于己方弃牌堆/手牌）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public StableRecall()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 获得格挡，按格挡牌参与力量/敏捷等计算。
	public override bool GainsBlock => true;

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/StableRecall.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：格挡 5（升级后 8），绑定 {Block:diff()} 占位符。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(5m, ValueProp.Move)
	];

	// 打出后自动消耗。
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

	// 悬停提示：稳定附魔（Steady：该牌获得保留）。
	// 注意：HoverTipFactory.FromEnchantment<T>() 本身返回 IEnumerable<IHoverTip>，不能再用集合表达式包一层。
	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
		HoverTipFactory.FromEnchantment<Steady>();

	// 打出时：先获得格挡，再从弃牌堆选一张能附魔稳定的牌放入手牌并附魔稳定。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

		// 1. 获得格挡。
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

		// 2. 从弃牌堆选择一张牌。filter 只放行能附魔稳定的牌（状态/诅咒等类型会被 CanEnchant 拒绝，
		//    已附魔的牌也会被排除），避免 CardCmd.Enchant 因 CanEnchant 失败而抛异常。
		CardModel? picked = (await CardSelectCmd.FromCombatPile(
			prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 1),
			context: choiceContext,
			pile: PileType.Discard.GetPile(Owner),
			player: Owner,
			filter: CanEnchantSteady)).FirstOrDefault();

		// 弃牌堆中没有可选的牌（例如空弃牌堆）时，跳过回收与附魔。
		if (picked == null)
		{
			return;
		}

		// 3. 放入手牌并附魔稳定（稳定 = Steady：该牌获得保留）。
		await CardPileCmd.Add(picked, PileType.Hand);
		CardCmd.Enchant<Steady>(picked, 1m);
	}

	// 升级：格挡 5 -> 8。
	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(3m);
	}

	// 与 CardCmd.Enchant 内部的 CanEnchant 检查一致：只允许选择能附魔稳定的牌。
	private static bool CanEnchantSteady(CardModel card)
	{
		return ModelDb.Enchantment<Steady>().ToMutable().CanEnchant(card);
	}
}
