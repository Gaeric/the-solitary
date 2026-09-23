using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 勤学（character.org）：任务牌，不能被打出；进入 5 种不同类型的房间后转换为 博识。开局作为初始卡加入牌组。
// 完全对照原版 探寻 Dowsing → 富足 Abundance 的转换关系：
//   - CardType.Quest / CardRarity.Quest / 费用 -1 / Unplayable / MaxUpgradeLevel = 0；
//   - 用 [SavedProperty] 位掩码保存"已进入过的房间类型"，BeforeRoomEntered 里累计，
//     集齐 5 种后 PlayerCmd.CompleteQuest + CardCmd.TransformTo<Erudition>（牌组原件被永久转换）；
//   - 悬停提示逐条列出"还没进入过的房间类型"（用原版房间名 + 本 Mod 说明），让玩家清楚还差哪些房间；
//   - 注册进原版 QuestCardPool（与探寻同池），因此不会出现在卡牌奖励 / 商店 / 随机变化里；
//   - 再用 RegisterCharacterStarterCard 把它作为初始卡加入本角色初始牌组（Order 2 = 排在打击/防御/唤醒/匣中术之后）。
[RegisterCard(typeof(QuestCardPool))]
[RegisterCharacterStarterCard(typeof(TheSolitaryCharacter), 1, Order = 2)]
public sealed class DiligentStudy : ModCardTemplate
{
	// 需要集齐的房间类型数量（character.org 指定的 5 种 AbstractRoom 房间）。
	public const int RequiredRoomTypes = 5;

	// 5 种房间类型各占位一位（位掩码）。
	private const int CombatRoomBit = 1 << 0;    // CombatRoom：普通敌人 / 精英 / Boss 战斗房
	private const int TreasureRoomBit = 1 << 1;  // TreasureRoom：宝箱房
	private const int MerchantRoomBit = 1 << 2;  // MerchantRoom：商店
	private const int RestSiteRoomBit = 1 << 3;  // RestSiteRoom：营火（休息处）
	private const int EventRoomBit = 1 << 4;     // EventRoom：事件（含先古事件）

	// 集齐这 5 种房间即转换（MapRoom 地图界面不是房间，不计入）。
	private const int RequiredRoomMask =
		CombatRoomBit | TreasureRoomBit | MerchantRoomBit | RestSiteRoomBit | EventRoomBit;

	// 悬停提示用：位 → 原版房间提示键（static_hover_tips 表里的 ROOM_*.title，
	// 直接复用原版房间名，中文/英文随游戏语言自动本地化）。
	private static readonly (int Bit, string RoomTipKey)[] RoomTips =
	[
		(CombatRoomBit, "ROOM_ENEMY"),
		(TreasureRoomBit, "ROOM_TREASURE"),
		(MerchantRoomBit, "ROOM_MERCHANT"),
		(RestSiteRoomBit, "ROOM_REST"),
		(EventRoomBit, "ROOM_EVENT")
	];

	// 基础耗能（任务牌不可打出，-1 与探寻 Dowsing 一致）。
	private const int BaseEnergyCost = -1;
	// 卡牌类型（任务牌）。
	private const CardType CardKind = CardType.Quest;
	// 卡牌稀有度（任务牌：不参与奖励稀有度骰子，与探寻一致）。
	private const CardRarity CardRarityValue = CardRarity.Quest;
	// 目标类型（不可打出 → 无目标）。
	private const TargetType CardTarget = TargetType.None;
	// 在卡牌图鉴中显示，方便玩家查阅任务规则。
	private const bool ShowInCardLibrary = true;

	// 已进入过的房间类型位掩码。
	private int _visitedRoomMask;

	public DiligentStudy()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/DiligentStudy.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 任务牌不可升级（与探寻 Dowsing 一致）。
	public override int MaxUpgradeLevel => 0;

	// 任务牌不应被"在战斗中生成卡牌"的效果抽到（如发现 / 攻击药水）。
	public override bool CanBeGeneratedInCombat => false;

	// 不能被打出（卡面自动显示"不能被打出"，因此描述里不再重复）。
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

	// 基础数值：还差几种房间类型（绑定 {Types:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("Types", RequiredRoomTypes)
	];

	// 悬停提示：
	// 1) 转换目标 博识 的卡牌预览（与探寻提示富足同款）；
	// 2) 逐条列出"还没进入过的房间类型"——标题用原版房间名（static_hover_tips 的 ROOM_*.title），
	//    说明用本 Mod 文案，方便玩家一眼看出还差哪些房间。集齐后这两类提示都会消失。
	protected override IEnumerable<IHoverTip> AdditionalHoverTips
	{
		get
		{
			yield return HoverTipFactory.FromCard<Erudition>();

			foreach ((int bit, string roomTipKey) in RoomTips)
			{
				if ((VisitedRoomMask & bit) != 0)
				{
					continue;
				}
				yield return new HoverTip(
					new LocString("static_hover_tips", roomTipKey + ".title"),
					new LocString("cards", "THE_SOLITARY_CARD_DILIGENT_STUDY.missingRoom.description"));
			}
		}
	}

	/// <summary>
	/// 已进入过的房间类型位掩码。<c>[SavedProperty]</c> 使进度随存档保存（与探寻把进度存在卡上一致）；
	/// setter 内同步刷新 {Types:diff()}（= 还差几种房间类型），让卡面描述实时反映进度。
	/// </summary>
	[SavedProperty]
	public int VisitedRoomMask
	{
		get => _visitedRoomMask;
		set
		{
			AssertMutable();
			_visitedRoomMask = value;
			DynamicVars["Types"].BaseValue = RequiredRoomTypes - CountCollectedRoomTypes(value);
		}
	}

	/// <summary>
	/// 进入房间时累计房间类型（参考原版探寻 Dowsing 的 BeforeRoomEntered）：
	/// 只有这张牌在[gold]牌组[/gold]里时才计数（战斗中抽到的克隆牌不算），
	/// 并只统计 character.org 指定的 5 种房间（战斗 / 宝箱 / 商店 / 营火 / 事件）。
	/// 集齐 5 种不同类型的房间后完成任务，并把这张牌转换为 博识。
	/// </summary>
	public override async Task BeforeRoomEntered(AbstractRoom room)
	{
		CardPile? pile = Pile;
		if (pile == null || pile.Type != PileType.Deck)
		{
			return;
		}

		int visited = VisitedRoomMask | RoomTypeBitFor(room);
		if (visited == VisitedRoomMask)
		{
			// 该类房间已经进过（或本次进入的是不计数的地方，如地图界面），无事发生。
			return;
		}
		VisitedRoomMask = visited;

		if ((visited & RequiredRoomMask) != RequiredRoomMask)
		{
			return;
		}

		// 集齐 5 种 → 完成任务并转换为 博识（与探寻 CompleteQuest + TransformTo 一致）。
		PlayerCmd.CompleteQuest(this);
		await CardCmd.TransformTo<Erudition>(this);
	}

	// 房间实例 → 位掩码。CombatRoom 覆盖普通/精英/Boss 三种战斗房；
	// MapRoom（地图界面）不是"房间"，返回 0（不计数）。
	private static int RoomTypeBitFor(AbstractRoom room)
	{
		return room switch
		{
			CombatRoom => CombatRoomBit,
			TreasureRoom => TreasureRoomBit,
			MerchantRoom => MerchantRoomBit,
			RestSiteRoom => RestSiteRoomBit,
			EventRoom => EventRoomBit,
			_ => 0
		};
	}

	// 已集齐的房间类型数量。
	private static int CountCollectedRoomTypes(int mask)
	{
		int count = 0;
		for (int bit = CombatRoomBit; bit <= EventRoomBit; bit <<= 1)
		{
			if ((mask & bit) != 0)
			{
				count++;
			}
		}
		return count;
	}
}
