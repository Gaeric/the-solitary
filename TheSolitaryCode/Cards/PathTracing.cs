using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 路径追踪（英文名 Path Tracing）：0 费技能。
// 在这个回合内每打出过一张技能牌，就对一名随机敌人打出一张随机术式（统计的是本牌打出前已结算的技能牌）。
// 技能计数参考原版 连击 Finisher：CombatManager.Instance.History.CardPlaysFinished + HappenedThisTurn；
// 随机术式生成与快速自动打出复用 Arts.CreateRandomInHandAndFastPlay（生成到手牌 + 快节奏自动打出）。
// 升级后打出的术式变为术式+（升级版）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class PathTracing : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 0;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（蓝卡 = Uncommon）。
	private const CardRarity CardRarityValue = CardRarity.Uncommon;
	// 目标类型（自身；术式会自动随机选择敌方目标）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public PathTracing()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/PathTracing.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 打出时：统计本回合已打出的技能牌数量，每张生成一张随机术式（升级后为术式+）并自动打出（随机敌方目标）。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

		// 本回合已打出的技能牌数（参考连击 Finisher：战斗历史中本回合已完成的打出记录）。
		int skillCount = CombatManager.Instance.History.CardPlaysFinished.Count(e =>
			e.HappenedThisTurn(CombatState!) &&
			e.CardPlay.Card.Type == CardType.Skill &&
			e.CardPlay.Player == Owner);

		ICombatState combatState = CombatState!;
		for (int i = 0; i < skillCount; i++)
		{
			// 生成一张随机术式到手牌并立即快速自动打出（保留进手牌动画，跳过冗长的牌堆移动/等待动画）。
			// 内部负责打出区节点清理，不会在 UI 中残留卡牌。
			await Arts.CreateRandomInHandAndFastPlay(
				Owner, combatState, Owner.RunState.Rng.CombatCardGeneration, choiceContext, upgraded: IsUpgraded);
		}
	}

	// 升级：打出的随机术式变为术式+（升级版）。
}
