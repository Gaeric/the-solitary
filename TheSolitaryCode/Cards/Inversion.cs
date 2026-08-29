using System;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 逆转（character.org todo 白卡）：1 费攻击。
// 对所有敌人造成 9 点伤害（升级后 13 点）；每当你洗牌 1 次，在本场战斗中其基础伤害 -2。
// 洗牌减伤参考原版 Claw / Rampage 直接改写 DynamicVar.BaseValue 的模式；
// 战斗中的卡牌是牌组卡的克隆（DynamicVarSet 各自独立，见 CardModel 克隆逻辑），
// 修改只作用于本场战斗的克隆，战斗结束自动失效，天然符合"本场战斗"语义。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Inversion : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（攻击）。
	private const CardType CardKind = CardType.Attack;
	// 卡牌稀有度（白卡 = Common）。
	private const CardRarity CardRarityValue = CardRarity.Common;
	// 目标类型（全体敌人）。
	private const TargetType CardTarget = TargetType.AllEnemies;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;
	// 每次洗牌减少的基础伤害。
	private const int DamageReductionPerShuffle = 2;

	public Inversion()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Inversion.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：伤害（绑定 {Damage:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(9m, ValueProp.Move)
	];

	// 抽牌堆被洗牌时触发：本场战斗内这张牌的基础伤害 -2（多次洗牌可叠加，战斗结束自动失效）。
	public override Task AfterShuffle(PlayerChoiceContext choiceContext, Player shuffler)
	{
		if (shuffler == Owner)
		{
			DynamicVars.Damage.BaseValue =
				Math.Max(0m, DynamicVars.Damage.BaseValue - DamageReductionPerShuffle);
		}
		return Task.CompletedTask;
	}

	// 打出时：对所有敌人造成伤害。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.TargetingAllOpponents(CombatState!)
			.Execute(choiceContext);
	}

	// 升级：伤害 9 -> 13。
	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(4m);
	}
}
