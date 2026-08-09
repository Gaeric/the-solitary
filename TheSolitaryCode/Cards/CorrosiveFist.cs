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
// 造成 6 点伤害，目标身上所有减益数值 +1（参考原版 熔岩之拳 MoltenFist 的减益层数操控方式）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class CorrosiveFist : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（攻击）。
	private const CardType CardKind = CardType.Attack;
	// 卡牌稀有度（白卡 = Common）。
	private const CardRarity CardRarityValue = CardRarity.Common;
	// 目标类型（任意敌人）。
	private const TargetType CardTarget = TargetType.AnyEnemy;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public CorrosiveFist()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/CorrosiveFist.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 打出后消耗（参考熔岩之拳）。
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

	// 基础数值：伤害（绑定 {Damage:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(6m, ValueProp.Move)
	];

	// 打出时：先造成 6 点伤害，再让目标身上的每种减益数值 +1。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target);

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.Execute(choiceContext);

		// 枚举目标身上所有 Debuff 类型的 Power，各 +1。
		// 用 ModelDb.DebugPower + ToMutable 生成可变原型传给 PowerCmd.Apply：
		// 已存在的减益会走叠加路径加层数；理论上可能存在的 Instanced 型减益也会安全地新增一层。
		foreach (PowerModel debuff in cardPlay.Target.Powers.Where(p => p.Type == PowerType.Debuff))
		{
			await PowerCmd.Apply(choiceContext,
				ModelDb.DebugPower(debuff.GetType()).ToMutable(),
				cardPlay.Target, 1m, Owner.Creature, this);
		}
	}

	// 升级：伤害 6 -> 9。
	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(3m);
	}
}
