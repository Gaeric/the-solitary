using System;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 涌泉（新增白卡，英文名 Wellspring）：0 费技能。
// 获得 1 点能量（升级后 2 点）；每当你洗牌 2 次，获得的能量减少 1（本场战斗内可叠加，最低 0）。
// 洗牌减益参考逆转 Inversion：AfterShuffle 钩子直接改写 DynamicVar.BaseValue，
// 战斗中的卡牌是牌组卡的克隆（DynamicVarSet 各自独立），修改只作用于本场战斗的克隆，战斗结束自动失效；
// 洗牌计数参考涌动 Surge：_shuffleCount 每洗牌 +1、BeforeCombatStart 清零（卡牌实例跨战斗复用）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Wellspring : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 0;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（白卡 = Common）。
	private const CardRarity CardRarityValue = CardRarity.Common;
	// 目标类型（自身）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;
	// 每洗牌多少次减少 1 点获得的能量。
	private const int ShufflesPerReduction = 2;

	// 本场战斗的洗牌次数（每当自己洗牌 +1，战斗开始前清零）。
	private int _shuffleCount;

	public Wellspring()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Wellspring.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：获得的能量（升级后 2），绑定 {Energy:energyIcons()} 占位符。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new EnergyVar(1)
	];

	// 抽牌堆被洗牌时触发：每洗牌 2 次，本场战斗内这张牌获得的能量 -1（最低 0，多次洗牌可叠加）。
	public override Task AfterShuffle(PlayerChoiceContext choiceContext, Player shuffler)
	{
		if (shuffler == Owner)
		{
			_shuffleCount++;
			if (_shuffleCount % ShufflesPerReduction == 0)
			{
				DynamicVars.Energy.BaseValue =
					Math.Max(0m, DynamicVars.Energy.BaseValue - 1m);
			}
		}
		return Task.CompletedTask;
	}

	// 战斗开始前清零洗牌计数，避免把上一场战斗的累计带到本场（卡牌实例跨战斗复用）。
	public override Task BeforeCombatStart()
	{
		_shuffleCount = 0;
		return Task.CompletedTask;
	}

	// 打出时：获得能量。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
	}

	// 升级：获得的能量 1 -> 2。
	protected override void OnUpgrade()
	{
		DynamicVars.Energy.UpgradeValueBy(1m);
	}
}
