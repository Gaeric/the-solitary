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

// 腐蚀之拳（character.org 白卡 #9）：1 费攻击，消耗。
// 造成 8 点伤害；若目标有负面效果，则额外造成一次伤害（参考原版 怨恨 Spite 的条件命中次数模式）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class CorrosiveFist : ModCardTemplate
{
	private const int BaseEnergyCost = 1;
	private const CardType CardKind = CardType.Attack;
	private const CardRarity CardRarityValue = CardRarity.Common;
	private const TargetType CardTarget = TargetType.AnyEnemy;
	private const bool ShowInCardLibrary = true;

	public CorrosiveFist()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 打出后消耗（参考熔岩之拳）。
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

	// 基础数值：伤害（绑定 {Damage:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(8m, ValueProp.Move)
	];

	// 打出时：先判定目标是否带负面效果（造成伤害前快照，避免敌人被击杀后无法判断），
	// 若有负面效果则攻击 2 次（每次伤害 = Damage），否则攻击 1 次（参考原版 怨恨 Spite 的条件命中次数模式）。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target);

		bool hasDebuff = cardPlay.Target.Powers.Any(DebuffHelpers.IsDebuff);

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.WithHitCount(hasDebuff ? 2 : 1)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.Execute(choiceContext);
	}

	// 升级：伤害 8 -> 11。
	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(3m);
	}
}
