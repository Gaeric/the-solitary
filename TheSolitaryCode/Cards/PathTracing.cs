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

// 路径追踪（character.org 蓝卡，英文名 Path Tracing）：1 费攻击。
// 在这个回合内每打出过一张技能牌，就对一名随机敌人打出一张随机术式（统计的是本牌打出前已结算的技能牌）。
// 技能计数参考原版 连击 Finisher：CombatManager.Instance.History.CardPlaysFinished + HappenedThisTurn；
// 随机术式生成与自动打出参考全知形态 ArcaneReturnPower：Arts.CreateRandomInHand + CardCmd.AutoPlay(随机目标)。
// 升级后效果不变（与基础版一致）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class PathTracing : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（攻击）。
	private const CardType CardKind = CardType.Attack;
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
			// 生成一张随机术式到手牌，并以随机敌方目标自动打出（参考全知形态 ArcaneReturnPower）。
			// 升级后生成术式+（升级版），与全知形态/掌中奇术的升级一致。
			CardModel art = await Arts.CreateRandomInHand(
				Owner, combatState, Owner.RunState.Rng.CombatCardGeneration, choiceContext, upgraded: IsUpgraded);
			await CardCmd.AutoPlay(choiceContext, art, null);
		}
	}

	// 升级：打出的随机术式变为术式+（升级版）。
}
