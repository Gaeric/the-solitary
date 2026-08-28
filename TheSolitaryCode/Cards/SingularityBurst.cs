using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Characters;
using TheSolitary.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 奇点爆破（自定义卡）：2 费攻击（升级后 1 费）。
// 本次战斗每生成过一张术式，造成 1 点伤害（单次伤害 = 术式生成数）；
// 敌人每有一种负面效果，额外造成一次伤害（命中次数 = 1 + 负面效果种类数）。
// 生成数由 ArtTrackerPower 在 Arts.CreateRandomInHand 中累计。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class SingularityBurst : ModCardTemplate
{
	// 命中次数对应的 DynamicVar 键名。
	private const string CalculatedHitsKey = "CalculatedHits";

	// 基础耗能。
	private const int BaseEnergyCost = 2;
	// 卡牌类型（攻击）。
	private const CardType CardKind = CardType.Attack;
	// 卡牌稀有度（蓝卡 = Uncommon）。
	private const CardRarity CardRarityValue = CardRarity.Uncommon;
	// 目标类型（任意敌人）。
	private const TargetType CardTarget = TargetType.AnyEnemy;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public SingularityBurst()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：
	// 单次伤害 = 基础(0) + ExtraDamage(1) × 本场战斗生成的术式数量（绑定 {CalculatedDamage:diff()} / {ExtraDamage:diff()}）。
	// 命中次数 = 基础 1 次 + 目标身上的负面效果类型数（绑定 {CalculatedHits:diff()}）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CalculationBaseVar(0m),
		new ExtraDamageVar(1m),
		new CalculationExtraVar(1m),
		new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (card, _) =>
			card.Owner.Creature.GetPowerAmount<ArtTrackerPower>()),
		new CalculatedVar(CalculatedHitsKey).WithMultiplier(static (_, target) =>
			1 + CountDistinctDebuffTypes(target))
	];

	// 打出时：按“单次伤害 × 命中次数”进行多段攻击
	//（至少 1 次 + 目标每种负面效果额外 1 次，参考原版 FlakCannon 的 WithHitCount 用法）。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target);

		int hits = (int)((CalculatedVar)DynamicVars[CalculatedHitsKey]).Calculate(cardPlay.Target);

		await DamageCmd.Attack(DynamicVars.CalculatedDamage)
			.WithHitCount(hits)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.Execute(choiceContext);
	}

	// 升级：费用 2 -> 1（伤害机制不变）。
	protected override void OnUpgrade()
	{
		base.EnergyCost.UpgradeBy(-1);
	}

	/// <summary>
	/// 统计目标身上不同的负面效果类型数（与瘟疫 Pestilence 相同的计数方式，参考原版 Rend）。
	/// </summary>
	private static int CountDistinctDebuffTypes(Creature? target)
	{
		if (target == null)
		{
			return 0;
		}
		return target.Powers
			.Where(p => p.TypeForCurrentAmount == PowerType.Debuff && p is not ITemporaryPower)
			.Select(p => p.Id)
			.Distinct()
			.Count();
	}
}
