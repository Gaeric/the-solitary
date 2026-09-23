using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 超渡（character.org）：任务牌，不能被打出；经历 7 场战斗后从牌组中移除，并获得 500 金币。开局作为初始卡加入牌组。
// 计数方式与原版诅咒 愧疚 Guilty 完全一致（它是原版唯一的"在 N 场战斗后从牌组移除"样板）：
//   - 覆写 AfterCombatEnd(CombatRoom)：战斗结束时 +1，只有牌在牌组里才计数（战斗中抽到的克隆牌不算）；
//   - [SavedProperty] 保存已历战斗数，{Combats:diff()} 显示"还差几场"（= RequiredCombats - 已历场数，同 Guilty）；
//   - 达成后 CardPileCmd.RemoveFromDeck 从牌组移除（要求牌确实在牌组里）。
// 结算顺序参考原版任务牌 宝藏图 SpoilsMap.OnQuestComplete：先给金币 → 记录任务完成 → 再移出牌组。
// 骨架与 勤学 DiligentStudy / 探寻 Dowsing 一致（任务牌 + 注册进原版 QuestCardPool），
// 并用 RegisterCharacterStarterCard 作为初始卡（Order 3 = 排在打击/防御/唤醒/匣中术/勤学之后）。
[RegisterCard(typeof(QuestCardPool))]
[RegisterCharacterStarterCard(typeof(TheSolitaryCharacter), 1, Order = 3)]
public sealed class Requiem : ModCardTemplate
{
	// 需要经历的战斗场数（对应原版 Guilty.maxCombats 的位置）。
	public const int RequiredCombats = 7;

	// 完成时给予的金币（绑定 {Gold:diff()}）。
	private const int RewardGold = 500;

	// 基础耗能（任务牌不可打出，-1 与探寻 / 愧疚一致）。
	private const int BaseEnergyCost = -1;
	// 卡牌类型（任务牌）。
	private const CardType CardKind = CardType.Quest;
	// 卡牌稀有度（任务牌：不参与奖励稀有度骰子）。
	private const CardRarity CardRarityValue = CardRarity.Quest;
	// 目标类型（不可打出 → 无目标）。
	private const TargetType CardTarget = TargetType.None;
	// 在卡牌图鉴中显示，方便玩家查阅任务规则。
	private const bool ShowInCardLibrary = true;

	// 已经历的战斗场数。
	private int _combatsSeen;

	public Requiem()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Requiem.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 任务牌不可升级（与探寻 Dowsing / 愧疚 Guilty 一致）。
	public override int MaxUpgradeLevel => 0;

	// 任务牌不应被"在战斗中生成卡牌"的效果抽到（如发现 / 攻击药水）。
	public override bool CanBeGeneratedInCombat => false;

	// 不能被打出（卡面自动显示"不能被打出"，因此描述里不再重复）。
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

	// 基础数值：还差几场战斗（绑定 {Combats:diff()}）+ 完成时获得的金币（绑定 {Gold:diff()}）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("Combats", RequiredCombats),
		new GoldVar(RewardGold)
	];

	/// <summary>
	/// 已经历的战斗场数。<c>[SavedProperty]</c> 使进度随存档保存；
	/// setter 内按 愧疚 Guilty 的同款公式刷新 {Combats:diff()}（= 还差几场战斗）。
	/// </summary>
	[SavedProperty]
	public int CombatsSeen
	{
		get => _combatsSeen;
		set
		{
			AssertMutable();
			_combatsSeen = value;
			DynamicVars["Combats"].BaseValue = RequiredCombats - value;
		}
	}

	/// <summary>
	/// 战斗结束时累计场数（与愧疚 Guilty 完全一致）：只有这张牌在[gold]牌组[/gold]里时才计数
	/// （战斗中抽到的克隆牌不算）。累计到 7 场后：获得 {Gold:diff()} 金币 → 记录任务完成 → 从牌组中移除。
	/// </summary>
	public override async Task AfterCombatEnd(CombatRoom room)
	{
		CardPile? pile = Pile;
		if (pile == null || pile.Type != PileType.Deck)
		{
			return;
		}

		CombatsSeen++;
		if (CombatsSeen < RequiredCombats)
		{
			return;
		}

		// 达成 7 场：先给金币 → 记录任务完成 → 再移出牌组（顺序同宝藏图 SpoilsMap.OnQuestComplete）。
		await PlayerCmd.GainGold(DynamicVars.Gold.BaseValue, Owner);
		PlayerCmd.CompleteQuest(this);

		// 与愧疚 Guilty 一样，移除前再确认牌还在牌组里（RemoveFromDeck 要求牌在牌组中）。
		if (Pile?.Type == PileType.Deck)
		{
			await CardPileCmd.RemoveFromDeck(this);
		}
	}
}
