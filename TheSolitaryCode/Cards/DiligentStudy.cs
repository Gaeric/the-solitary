using System;
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

// 勤学（character.org）：任务牌，不能被打出；进入 5 种不同类型的房间各 2 次后转换为 博识。开局作为初始卡加入牌组。
// 卡牌定位：**任务牌**（CardType.Quest + CardRarity.Quest + 注册进原版 QuestCardPool，与探寻 Dowsing 同池）
// → 不进卡牌奖励 / 商店 / 战斗生成 / 随机变化。
// 历史：2026-09 曾一度改为基础牌（CardType.Skill + CardRarity.Basic + 角色卡池），以便被木雕 WoodCarvings 这类
// "针对基础牌"的效果变换；随后按要求改回任务牌，因此卡面与行为都以任务牌为准。
// 转换关系完全对照原版 探寻 Dowsing → 富足 Abundance：
//   - 费用 -1 / Unplayable / MaxUpgradeLevel = 0 / CanBeGeneratedInCombat = false；
//   - 用 [SavedProperty] int RoomVisits 存"每种房型已经进入过几次"（每种 2 位、5 种共 10 位），
//     BeforeRoomEntered 累计：5 种房型**各进入 2 次**（共 10 次）后 PlayerCmd.CompleteQuest +
//     CardCmd.TransformTo<Erudition>（牌组原件被永久转换）；
//   - 悬停提示逐条列出"还没进满 2 次的房型"并写明还差几次（用原版房间名 + 本 Mod 说明）；
//   - 注册进原版 QuestCardPool（与探寻同池），因此不会出现在卡牌奖励 / 商店 / 随机变化里；
//   - 再用 RegisterCharacterStarterCard 把它作为初始卡加入本角色初始牌组（Order 2 = 排在打击/防御/唤醒/匣中术之后）。
[RegisterCard(typeof(QuestCardPool))]
[RegisterCharacterStarterCard(typeof(TheSolitaryCharacter), 1, Order = 2)]
public sealed class DiligentStudy : ModCardTemplate
{
	// 需要集齐的房间类型数量（character.org 指定的 5 种 AbstractRoom 房间）。
	public const int RequiredRoomTypes = 5;

	// 每种房间类型需要进入的次数（character.org：改成"5 种不同类型的房间各 2 次"）。
	public const int RequiredVisitsPerType = 2;

	// 总共需要进入的次数（5 种 × 各 2 次 = 10），也是 {Times:diff()} 的初值。
	public const int TotalRequiredVisits = RequiredRoomTypes * RequiredVisitsPerType;

	// 5 种房间类型在访问计数里的下标（每种房型占一个计数位）。
	private const int CombatRoomIndex = 0;    // CombatRoom：普通敌人 / 精英 / Boss 战斗房
	private const int TreasureRoomIndex = 1;  // TreasureRoom：宝箱房
	private const int MerchantRoomIndex = 2;  // MerchantRoom：商店
	private const int RestSiteRoomIndex = 3;  // RestSiteRoom：营火（休息处）
	private const int EventRoomIndex = 4;     // EventRoom：事件（含先古事件）

	// 每种房型占 2 个二进制位（存 0~2 次），5 种房型共 10 位，全部塞进一个 [SavedProperty] int。
	// 注意：这与旧版"1 位 1 类型"的编码语义不同，所以刻意换用新属性名 RoomVisits——
	// SavedProperties.FillInternal 是按属性名反射赋值（名字对不上就静默忽略），换名不会让旧存档报错，
	// 而沿用旧名会把旧位掩码误读成访问次数。
	private const int BitsPerRoomType = 2;
	private const int VisitCountMask = (1 << BitsPerRoomType) - 1;  // 0b11

	// 悬停提示用：房型下标 → 原版房间提示键（static_hover_tips 表里的 ROOM_*.title，
	// 直接复用原版房间名，中文/英文随游戏语言自动本地化）。
	private static readonly (int Index, string RoomTipKey)[] RoomTips =
	[
		(CombatRoomIndex, "ROOM_ENEMY"),
		(TreasureRoomIndex, "ROOM_TREASURE"),
		(MerchantRoomIndex, "ROOM_MERCHANT"),
		(RestSiteRoomIndex, "ROOM_REST"),
		(EventRoomIndex, "ROOM_EVENT")
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

	// 已进入过的各类房间次数（每种房型 2 位）。
	private int _roomVisits;

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

	// 基础数值：
	//   {Types:diff()}  = 需要集齐的房间类型数（5，固定）；
	//   {Visits:diff()} = 每种房型需要进入的次数（2，固定）；
	//   {Times:diff()}  = 还差几次进入（10 → 0，随进度实时刷新）。
	// 注意：当前卡面文案（zhs / eng）用固定措辞描述规则（"所有类型的房间各 2 次"），并未引用这三个占位符，
	// 进度改由悬停提示逐条展示（还差 2 次 / 1 次）。三个 var 保留是有意为之：以后想在卡面显示
	// "还差{Times:diff()}次" 时直接加进描述文本即可——RoomVisits 的 setter 一直在实时刷新它的值。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DynamicVar("Types", RequiredRoomTypes),
		new DynamicVar("Visits", RequiredVisitsPerType),
		new DynamicVar("Times", TotalRequiredVisits)
	];

	// 悬停提示：
	// 1) 转换目标 博识 的卡牌预览（与探寻提示富足同款）；
	// 2) 逐条列出"还没进满 2 次的房型"——标题用原版房间名（static_hover_tips 的 ROOM_*.title），
	//    说明按"还差 2 次 / 还差 1 次"选本 Mod 文案，方便玩家一眼看出还差哪些房间、各差几次；
	//    全部进满后这两类提示都会消失。
	protected override IEnumerable<IHoverTip> AdditionalHoverTips
	{
		get
		{
			yield return HoverTipFactory.FromCard<Erudition>();

			foreach ((int index, string roomTipKey) in RoomTips)
			{
				int remaining = RequiredVisitsPerType - VisitCountAt(RoomVisits, index);
				if (remaining <= 0)
				{
					continue;
				}
				// 每种房型恰好需要 2 次，所以"还差几次"只有 2 / 1 两种取值，用两条静态文案即可。
				string missingKey = (remaining >= RequiredVisitsPerType)
					? "THE_SOLITARY_CARD_DILIGENT_STUDY.missingRoomTwice.description"
					: "THE_SOLITARY_CARD_DILIGENT_STUDY.missingRoomOnce.description";
				yield return new HoverTip(
					new LocString("static_hover_tips", roomTipKey + ".title"),
					new LocString("cards", missingKey));
			}
		}
	}

	/// <summary>
	/// 已进入过的各类房间次数（每种房型 2 位：0~2 次）。<c>[SavedProperty]</c> 使进度随存档保存
	/// （与探寻把进度存在卡上一致）；setter 内同步刷新 {Times:diff()}（= 还差几次进入），
	/// 让卡面描述实时反映进度。
	/// </summary>
	[SavedProperty]
	public int RoomVisits
	{
		get => _roomVisits;
		set
		{
			AssertMutable();
			_roomVisits = value;
			DynamicVars["Times"].BaseValue = TotalRequiredVisits - CountTotalVisits(value);
		}
	}

	/// <summary>
	/// 进入房间时累计对应房型的次数（参考原版探寻 Dowsing 的 BeforeRoomEntered）：
	/// 只有这张牌在[gold]牌组[/gold]里时才计数（战斗中抽到的克隆牌不算），
	/// 且只统计 character.org 指定的 5 种房间（战斗 / 宝箱 / 商店 / 营火 / 事件），
	/// 每种最多记 2 次（进满后再进同类房间不计数）。
	/// 5 种房型各进满 2 次（共 10 次）后完成任务，并把这张牌转换为 博识。
	/// </summary>
	public override async Task BeforeRoomEntered(AbstractRoom room)
	{
		CardPile? pile = Pile;
		if (pile == null || pile.Type != PileType.Deck)
		{
			return;
		}

		int index = RoomTypeIndexFor(room);
		if (index < 0)
		{
			// MapRoom（地图界面）等不是"房间"，不计入。
			return;
		}

		int visits = VisitCountAt(RoomVisits, index);
		if (visits >= RequiredVisitsPerType)
		{
			// 这种房型已经进满 2 次，不再计数。
			return;
		}

		RoomVisits = WithVisitCountAt(RoomVisits, index, visits + 1);
		if (CountTotalVisits(RoomVisits) < TotalRequiredVisits)
		{
			return;
		}

		// 5 种房型各进满 2 次 → 完成任务并转换为 博识（与探寻 CompleteQuest + TransformTo 一致）。
		PlayerCmd.CompleteQuest(this);
		await CardCmd.TransformTo<Erudition>(this);
	}

	// 房间实例 → 计数下标（-1 = 不计数）。CombatRoom 覆盖普通/精英/Boss 三种战斗房。
	private static int RoomTypeIndexFor(AbstractRoom room)
	{
		return room switch
		{
			CombatRoom => CombatRoomIndex,
			TreasureRoom => TreasureRoomIndex,
			MerchantRoom => MerchantRoomIndex,
			RestSiteRoom => RestSiteRoomIndex,
			EventRoom => EventRoomIndex,
			_ => -1
		};
	}

	// 读取第 index 种房型已经进入过的次数（读时夹紧到上限，避免存档被改坏后 {Times:diff()} 出现负数）。
	private static int VisitCountAt(int visits, int index)
	{
		int count = (visits >> (index * BitsPerRoomType)) & VisitCountMask;
		return Math.Min(count, RequiredVisitsPerType);
	}

	// 把第 index 种房型的次数改写为 count，其余房型的位保持不变。
	private static int WithVisitCountAt(int visits, int index, int count)
	{
		int shift = index * BitsPerRoomType;
		return (visits & ~(VisitCountMask << shift)) | (count << shift);
	}

	// 已累计的进入次数总和（= TotalRequiredVisits - {Times:diff()}）。
	private static int CountTotalVisits(int visits)
	{
		int total = 0;
		for (int index = 0; index < RequiredRoomTypes; index++)
		{
			total += VisitCountAt(visits, index);
		}
		return total;
	}
}
