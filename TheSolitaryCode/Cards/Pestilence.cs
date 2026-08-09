using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 瘟疫（character.org 蓝卡 #2）：1 费攻击。
// 目标身上每有一种减益类型，便造成 6 点伤害（按“不同的减益类型”计数，参考原版 撕裂 Rend 的 CalculatedDamageVar）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Pestilence : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（攻击）。
	private const CardType CardKind = CardType.Attack;
	// 卡牌稀有度（蓝卡 = Uncommon）。
	private const CardRarity CardRarityValue = CardRarity.Uncommon;
	// 目标类型（任意敌人）。
	private const TargetType CardTarget = TargetType.AnyEnemy;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public Pestilence()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Pestilence.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：计算伤害 = 基础(0) + 每种减益类型 × ExtraDamage(6)。
	// 绑定 {CalculatedDamage:diff()} / {ExtraDamage:diff()} 占位符（参考原版 Rend）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CalculationBaseVar(0m),
		new ExtraDamageVar(6m),
		new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (_, target) =>
			target == null
				? 0m
				: target.Powers
					.Where(ShouldCountPower)
					.Select(p => p.Id)
					.Distinct()
					.Count())
	];

	// 打出时：按计算出的总伤害攻击目标。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target);

		await DamageCmd.Attack(DynamicVars.CalculatedDamage)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.Execute(choiceContext);
	}

	// 升级：每种减益类型造成的伤害 6 -> 9。
	protected override void OnUpgrade()
	{
		DynamicVars.ExtraDamage.UpgradeValueBy(3m);
	}

	/// <summary>
	/// 计数条件：当前数值为减益的 Power；排除临时性 Power（参考原版 Rend，
	/// 避免临时减益与内层正式减益重复计数）。
	/// </summary>
	private static bool ShouldCountPower(PowerModel power)
	{
		return power.TypeForCurrentAmount == PowerType.Debuff && power is not ITemporaryPower;
	}
}
