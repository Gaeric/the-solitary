using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheSolitary.Characters;
using TheSolitary.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 元能吸附（character.org 蓝卡 #3）：2 费能力牌，升级后 1 费。
// 每打出 5 张附魔牌，获得 1 点能量。
// 实现参考环回形态 LoopForm（能力牌 + Power 内部计数阈值结算能量的模式）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class EnergyAbsorption : ModCardTemplate
{
	private const int BaseEnergyCost = 2;
	private const CardType CardKind = CardType.Power;
	private const CardRarity CardRarityValue = CardRarity.Uncommon;
	private const TargetType CardTarget = TargetType.Self;
	private const bool ShowInCardLibrary = true;

	public EnergyAbsorption()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：每次结算获得的能量（绑定 {Energy:energyIcons()} 占位符；与 Power 每层 Amount 保持一致）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new EnergyVar(1)
	];

	// 打出时：给自己施加元能吸附 Power（层数 = 每次结算获得的能量；附魔牌阈值固定为 5）。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
		await PowerCmd.Apply<EnergyAbsorptionPower>(choiceContext, Owner.Creature, DynamicVars.Energy.BaseValue, Owner.Creature, this);
	}

	// 升级：费用 2 -> 1（能量与阈值不变）。
	protected override void OnUpgrade()
	{
		base.EnergyCost.UpgradeBy(-1);
	}
}
