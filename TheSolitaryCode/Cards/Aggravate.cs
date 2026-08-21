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

// 恶化（character.org 蓝卡 #6）：1 费攻击。
// 造成 8 点伤害，目标身上所有减益数值 +1（升级后伤害 +2、减益数值 +2）。
// 参考熔岩之拳 MoltenFist（减益层数操控）与腐蚀之拳 CorrosiveFist（枚举 Debuff 型 Power 逐个 +1）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Aggravate : ModCardTemplate
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
	// 减益数值增量的 DynamicVar 键名（绑定 {DebuffAmount:diff()} 占位符）。
	private const string DebuffAmountKey = "DebuffAmount";
	// 基础减益数值增量。
	private const int DebuffAmount = 1;

	public Aggravate()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Aggravate.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：伤害 8 + 减益数值增量 1（绑定 {Damage:diff()} / {DebuffAmount:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(8m, ValueProp.Move),
		new DynamicVar(DebuffAmountKey, DebuffAmount)
	];

	// 打出时：先造成 8 点伤害，再让目标身上的每种减益数值 +DebuffAmount。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target);

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.Execute(choiceContext);

		// 枚举目标身上所有 Debuff 类型的 Power，各 +DebuffAmount（同腐蚀之拳的减益层数操控方式）。
		// 用 ModelDb.DebugPower + ToMutable 生成可变原型传给 PowerCmd.Apply：
		// 已存在的减益会走叠加路径加层数；理论上可能存在的 Instanced 型减益也会安全地新增一层。
		foreach (PowerModel debuff in cardPlay.Target.Powers.Where(p => p.Type == PowerType.Debuff))
		{
			await PowerCmd.Apply(choiceContext,
				ModelDb.DebugPower(debuff.GetType()).ToMutable(),
				cardPlay.Target, DynamicVars[DebuffAmountKey].BaseValue, Owner.Creature, this);
		}
	}

	// 升级：伤害 8 -> 10，减益数值增量 1 -> 2。
	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(2m);
		DynamicVars[DebuffAmountKey].UpgradeValueBy(1m);
	}
}
