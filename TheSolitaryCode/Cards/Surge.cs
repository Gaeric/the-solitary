using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 涌动（character.org 蓝卡 #1）：1 费技能。
// 获得 6 点格挡（升级后 8）；每当你洗牌一次，额外获得一次等量格挡（前后数值相同）。
// 格挡数值先结算敏捷/附魔效果，再计算次数：每次 GainBlock 都走完整 ModifyBlock 管线后累加。
// 洗牌次数由 AfterShuffle 累计、BeforeCombatStart 清零；hovertip 备注当前次数。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Surge : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（蓝卡 = Uncommon）。
	private const CardRarity CardRarityValue = CardRarity.Uncommon;
	// 目标类型（自身）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	// 本场战斗的洗牌次数（每当自己洗牌 +1，战斗开始前清零）。
	private int _shuffleCount;

	public Surge()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 获得格挡，按格挡牌参与力量/敏捷等计算。
	public override bool GainsBlock => true;

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Surge.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// hovertip 本地化键（cards 表中的自定义键）。
	private const string HoverTipTitleKey = "THE_SOLITARY_CARD_SURGE.hoverTipTitle";
	private const string HoverTipDescriptionKey = "THE_SOLITARY_CARD_SURGE.hoverTipDescription";
	// hovertip 中次数占位符的 DynamicVar 键名。
	private const string CountKey = "Count";

	// 基础数值：每次格挡值（升级后 8），绑定 {Block:diff()} 占位符。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(6m, ValueProp.Move)
	];

	// hovertip：每次悬停时重建，展示当前洗牌次数（次数 = 额外获得格挡的次数）。
	protected override IEnumerable<IHoverTip> AdditionalHoverTips
	{
		get
		{
			LocString title = new LocString("cards", HoverTipTitleKey);
			LocString description = new LocString("cards", HoverTipDescriptionKey);
			DynamicVar count = new DynamicVar(CountKey, _shuffleCount);
			title.Add(count);
			description.Add(count);
			return [new HoverTip(title, description)];
		}
	}

	// 抽牌堆被洗牌时触发：累计自己洗牌次数。
	// 战斗初始洗牌走 CardPile.Shuffle（仅触发 ModifyShuffleOrder），不会触发本钩子，不纳入计数。
	public override Task AfterShuffle(PlayerChoiceContext choiceContext, Player shuffler)
	{
		if (shuffler == Owner)
		{
			_shuffleCount++;
		}
		return Task.CompletedTask;
	}

	// 战斗开始前清零洗牌次数，避免把上一场战斗的累计带到本场（卡牌实例跨战斗复用）。
	public override Task BeforeCombatStart()
	{
		_shuffleCount = 0;
		return Task.CompletedTask;
	}

	// 打出时：基础获得一次格挡，外加每次洗牌额外一次（共 1 + 洗牌次数 次）。
	// 每次 GainBlock 都独立结算敏捷/附魔后再累加，即“先结算数值、再乘以次数”。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

		int totalGains = 1 + _shuffleCount;
		for (int i = 0; i < totalGains; i++)
		{
			await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay, fast: true);
		}
	}

	// 升级：格挡 6 -> 8（前后数值保持一致）。
	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(2m);
	}
}
