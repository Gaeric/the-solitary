using System;
using System.Collections.Generic;
using System.Linq;
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

// 移位（character.org 白卡 #1）：0 费攻击。
// 造成 3 点伤害（升级后 4 点）；手牌中每有 1 张附魔牌，本场战斗中所有移位卡牌的伤害 +1（升级后 +2）。
// 实现完全参考原版 爪击 Claw：
//   - 用 DynamicVar("Increase") 承载"每张附魔牌带来的加伤"（描述里的 {Increase:diff()}），升级时同时提升伤害与加伤；
//   - 先结算本次伤害，再把加成写给"本场战斗中所有同名卡"（PlayerCombatState.AllCards.OfType<EnchantImpact>()）；
//   - 加成只作用于战斗克隆（牌组原件不受影响），战斗结束自动失效，天然符合"本场战斗"语义（同 逆转 Inversion 的说明）；
//   - 记录累计加成并在 AfterDowngraded 里补回（降级会重置 DynamicVars，与原版 Claw 同款处理）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class EnchantImpact : ModCardTemplate
{
	// "每张手牌附魔牌带来的加伤"对应的 DynamicVar 键名（与爪击的 Increase 同名）。
	private const string IncreaseKey = "Increase";

	// 基础耗能（0 费）。
	private const int BaseEnergyCost = 0;
	// 卡牌类型（攻击）。
	private const CardType CardKind = CardType.Attack;
	// 卡牌稀有度（白卡 = Common）。
	private const CardRarity CardRarityValue = CardRarity.Common;
	// 目标类型（任意敌人）。
	private const TargetType CardTarget = TargetType.AnyEnemy;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	// 由本卡叠加到自身伤害上的累计值（降级会重置 DynamicVars，需要靠它补回，参考 Claw.ExtraDamageFromClawPlays）。
	private decimal _extraDamageFromShiftPlays;

	public EnchantImpact()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/EnchantImpact.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：
	// - Damage：伤害 3（升级 4），绑定 {Damage:diff()}；
	// - Increase：手牌每 1 张附魔牌带来的加伤 1（升级 2），绑定 {Increase:diff()}。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(3m, ValueProp.Move),
		new DynamicVar(IncreaseKey, 1m)
	];

	// 打出时：先造成伤害，再按手牌附魔牌数量给本场战斗的所有移位加伤（参考 Claw.OnPlay）。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target);

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.Execute(choiceContext);

		// 每有 1 张手牌附魔牌，本场战斗中所有移位 +Increase（打出后生效，与爪击一致：本次伤害不吃本次加成）。
		decimal increase = DynamicVars[IncreaseKey].BaseValue * CountEnchantedCardsInHand(this);
		if (increase <= 0m)
		{
			return;
		}

		foreach (EnchantImpact shift in Owner.PlayerCombatState!.AllCards.OfType<EnchantImpact>())
		{
			shift.BuffFromShiftPlay(increase);
		}
	}

	// 升级：伤害 3 -> 4；每张附魔牌的加伤 1 -> 2（与爪击的 OnUpgrade 结构一致）。
	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(1m);
		DynamicVars[IncreaseKey].UpgradeValueBy(1m);
	}

	// 降级会重置 DynamicVars（DowngradeInternal 从 canonical 重新克隆数值），把累计加成补回去（同 Claw.AfterDowngraded）。
	protected override void AfterDowngraded()
	{
		DynamicVars.Damage.BaseValue += _extraDamageFromShiftPlays;
	}

	// 给这张移位加伤：只作用于本场战斗的克隆实例（参考 Claw.BuffFromClawPlay）。
	private void BuffFromShiftPlay(decimal extraDamage)
	{
		DynamicVars.Damage.BaseValue += extraDamage;
		_extraDamageFromShiftPlays += extraDamage;
	}

	// 手牌中附魔牌的数量。非战斗（卡牌图鉴等）预览时没有战斗状态，按 0 计算，避免预览报错。
	private static int CountEnchantedCardsInHand(CardModel card)
	{
		return card.Owner?.PlayerCombatState is { } combatState
			? combatState.Hand.Cards.Count(c => c.Enchantment != null)
			: 0;
	}
}
