using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 循迹（参考原版精密瞄准 Pinpoint）：3 费攻击。
// 造成 16 点伤害（升级后 20 点）。你在本回合中每打出过一张附魔牌，此牌耗能减少 1。
// 费用按“本回合”递减（每回合重置），与 Pinpoint 的 EnergyCost.AddThisTurn 一致。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Trace : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 3;
	// 卡牌类型（攻击）。
	private const CardType CardKind = CardType.Attack;
	// 卡牌稀有度（白卡 = Common）。
	private const CardRarity CardRarityValue = CardRarity.Common;
	// 目标类型（任意敌人）。
	private const TargetType CardTarget = TargetType.AnyEnemy;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public Trace()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Trace.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：伤害 16（绑定 {Damage:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(16m, ValueProp.Move)
	];

	// 打出时：造成 16 点伤害。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target);

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.Execute(choiceContext);
	}

	// 本卡进入战斗时，按本回合已打出的附魔牌数量立即减费（参考 Pinpoint.AfterCardEnteredCombat，
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

	// 每打出一张附魔牌，本回合内此牌耗能 -1（参考 Pinpoint.AfterCardPlayed）。
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

	// 升级：伤害 16 -> 20。
	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(4m);
	}
}
