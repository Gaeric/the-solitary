using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 超采样（character.org todo 蓝卡）：3 费技能。
// 获得 1 点能量（升级后 2 点）；你在本回合中每打出过一张附魔牌，此牌耗能减少 1。
// 费用按“本回合”递减（每回合重置），实现参考循迹 Trace / 原版精密瞄准 Pinpoint。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Supersampling : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 3;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（蓝卡 = Uncommon）。
	private const CardRarity CardRarityValue = CardRarity.Uncommon;
	// 目标类型（自身）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public Supersampling()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Supersampling.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：获得的能量（绑定 {Energy:energyIcons()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new EnergyVar(1)
	];

	// 打出时：获得能量。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
	}

	// 本卡进入战斗时，按本回合已打出的附魔牌数量立即减费（参考 Trace.AfterCardEnteredCombat，
	// 处理本卡较晚才抽到/进入战斗的情况）。
	public override Task AfterCardEnteredCombat(CardModel card)
	{
		if (card != this || base.IsClone)
		{
			return Task.CompletedTask;
		}
		int amount = CombatManager.Instance.History.CardPlaysFinished
			.Count((CardPlayFinishedEntry e) =>
				e.CardPlay.Card.Enchantment != null && e.CardPlay.Player == base.Owner && e.HappenedThisTurn(base.CombatState));
		ReduceCostBy(amount);
		return Task.CompletedTask;
	}

	// 每打出一张附魔牌，本回合内此牌耗能 -1（参考 Trace.AfterCardPlayed）。
	public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner != base.Owner || cardPlay.Card.Enchantment == null)
		{
			return Task.CompletedTask;
		}
		ReduceCostBy(1);
		return Task.CompletedTask;
	}

	// 本回合内降低耗能（每回合自动重置，参考 Pinpoint 的费用递减方式）。
	private void ReduceCostBy(int amount)
	{
		base.EnergyCost.AddThisTurn(-amount);
	}

	// 升级：能量 1 -> 2（费用与减费机制不变）。
	protected override void OnUpgrade()
	{
		DynamicVars.Energy.UpgradeValueBy(1);
	}
}
