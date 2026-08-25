using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 涟漪（character.org 白卡 #14）：1 费技能。
// 获得 5 点格挡；若本回合洗过牌，随机为手牌中一张牌附魔（升级后格挡 8）。
// “本回合洗过牌”由卡牌自身覆写 AfterShuffle / BeforeSideTurnStart 钩子记录：
// 卡牌在任意牌堆（手牌/抽牌堆/弃牌堆/消耗堆）中都会收到战斗钩子
// （CombatState.IterateHookListeners 会遍历玩家所有牌堆里的全部卡牌）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Ripple : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（白卡 = Common）。
	private const CardRarity CardRarityValue = CardRarity.Common;
	// 目标类型（自身）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	// 本回合是否洗过牌（卡牌实例级标志，玩家回合开始时重置）。
	private bool _shuffledThisTurn;

	public Ripple()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 获得格挡，按格挡牌参与力量/敏捷等计算。
	public override bool GainsBlock => true;

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Ripple.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：格挡 5（升级后 8），绑定 {Block:diff()} 占位符。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(5m, ValueProp.Move)
	];

	// 抽牌堆被洗牌时触发：只记录“自己洗牌”，不处理其它玩家的洗牌。
	public override Task AfterShuffle(PlayerChoiceContext choiceContext, Player shuffler)
	{
		if (shuffler == Owner)
		{
			_shuffledThisTurn = true;
		}
		return Task.CompletedTask;
	}

	// 玩家回合开始时重置洗牌标记，使“本回合”判定以回合为单位
	// （排除上一回合的洗牌与战斗初始洗牌）。仅在自己的回合开始时重置（participants 为当前行动方）。
	public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side,
		IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		if (side == CombatSide.Player && participants.Contains(Owner.Creature))
		{
			_shuffledThisTurn = false;
		}
		return Task.CompletedTask;
	}

	// 本回合已洗牌时卡牌泛金光，提示附魔部分会生效（参考原版 恶毒之眼 Evil Eye 的条件金光）。
	protected override bool ShouldGlowGoldInternal => _shuffledThisTurn;

	// 打出时：获得格挡；若本回合洗过牌，随机为手牌中一张未附魔的牌附魔。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

		if (!_shuffledThisTurn)
		{
			return;
		}

		// 随机附魔候选：手牌中未附魔的牌（参考唤醒 Sacrifice 的候选筛选）。
		// OnPlay 必然处于战斗中，PlayerCombatState 一定存在。
		List<CardModel> candidates = Owner.PlayerCombatState!.Hand.Cards
			.Where(card => card.Enchantment == null)
			.ToList();
		if (candidates.Count == 0)
		{
			return;
		}

		CardModel? target = Owner.RunState.Rng.CombatCardSelection.NextItem(candidates);
		if (target != null)
		{
			RandomEnchantPool.EnchantRandomly(Owner.RunState.Rng.CombatCardSelection, target);
		}
	}

	// 升级：格挡 5 -> 8。
	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(3m);
	}
}
