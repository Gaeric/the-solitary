using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 启示（character.org anch #2，替代已删除的黏糊魔典 StickyGrimoire）：0 费技能。
// 从抽牌堆中选择 1 张牌打出。升级后选择 2 张。消耗。
// 实现参考原版 灾变 Catastrophe / 喧哗 Uproar（CardCmd.AutoPlay 打出抽牌堆中的牌）
// ＋ 冲锋！！Charge（CardSelectCmd.FromCombatPile 从抽牌堆选牌，用 {Cards:diff()} 表达数量）。
// 注意：AutoPlay 是"免费打出"（不消耗能量、目标卡牌自身无需可打出资源），
// 目标类型为 AnyEnemy 的牌会随机选一个敌人（与原版 Catastrophe / Uproar 一致）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Revelation : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 0;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（先古 = Ancient）。
	private const CardRarity CardRarityValue = CardRarity.Ancient;
	// 目标类型（Self：只作用于己方抽牌堆）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public Revelation()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Revelation.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：打出张数 1（升级后 2），绑定 {Cards:diff()} 占位符。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(1)
	];

	// 打出后自动消耗。
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

	// 打出时：从抽牌堆中选择 1 张牌（升级后 2 张）并免费打出。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

		// 从抽牌堆选择。filter 排除不可打出的牌（状态/诅咒/任务牌等都带 Unplayable）：
		// AutoPlay 对 Unplayable 牌只会把它移出抽牌堆而不打出，与"选择牌打出"的语义不符。
		// FromCombatPile 自动处理数量：0 张可用 -> 空选择（跳过）；不足要求张数 -> 直接返回全部；
		// 足够时弹选择界面恰好选 {Cards} 张。
		List<CardModel> selection = (await CardSelectCmd.FromCombatPile(
			prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, DynamicVars.Cards.IntValue),
			context: choiceContext,
			pile: PileType.Draw.GetPile(Owner),
			player: Owner,
			filter: CanBePlayed)).ToList();

		// 依次打出：AutoPlay 会把牌从所在牌堆移入 Play 区并结算（不消耗能量）。
		foreach (CardModel card in selection)
		{
			await CardCmd.AutoPlay(choiceContext, card, null);
		}
	}

	// 升级：从抽牌堆中选择 1 张 -> 2 张。
	protected override void OnUpgrade()
	{
		DynamicVars.Cards.UpgradeValueBy(1m);
	}

	// 只放行能被打出的牌（AutoPlay 会跳过 Unplayable 牌）。
	private static bool CanBePlayed(CardModel card)
	{
		return !card.Keywords.Contains(CardKeyword.Unplayable);
	}
}
