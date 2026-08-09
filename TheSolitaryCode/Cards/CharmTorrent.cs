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

// 符潮（自定义卡）：2 费攻击。
// 造成“本场战斗中生成的减益符数量”点伤害；目标每有一种减益效果，就额外攻击一次。
// 生成数由 DebuffCharmTrackerPower 在 DebuffCharms.CreateRandomInHand 中累计。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class CharmTorrent : ModCardTemplate
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

	public CharmTorrent()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/CharmTorrent.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：
	// 单次伤害 = 基础(0) + ExtraDamage(1) × 本场战斗生成的减益符数量（绑定 {CalculatedDamage:diff()} / {ExtraDamage:diff()}）。
	// 命中次数 = 基础(0) + CalculationExtra(1) × (1 + 目标身上的减益类型数)（绑定 {CalculatedHits:diff()}）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CalculationBaseVar(0m),
		new ExtraDamageVar(1m),
		new CalculationExtraVar(1m),
		new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (card, _) =>
			card.Owner.Creature.GetPowerAmount<DebuffCharmTrackerPower>()),
		new CalculatedVar(CalculatedHitsKey).WithMultiplier(static (_, target) =>
			1 + CountDistinctDebuffTypes(target))
	];

	// 打出时：按“单次伤害 × 命中次数”进行多段攻击（参考原版 FlakCannon 的 WithHitCount 用法）。
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

	// 升级：每张减益符造成的伤害 1 -> 2。
	protected override void OnUpgrade()
	{
		DynamicVars.ExtraDamage.UpgradeValueBy(1m);
	}

	/// <summary>
	/// 统计目标身上不同的减益类型数（与瘟疫 Pestilence 相同的计数方式，参考原版 Rend）。
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
