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

// 焚尽（character.org 蓝卡 #23 变体——由"擦除手牌全部附魔"改为"消耗未附魔牌"）：2 费攻击。
// 消耗手牌中所有未附魔的卡牌，对所有敌人造成 21 点伤害（升级后 26 点）。
// 全体伤害参考原版硬着陆 CrashLanding（DamageVar 21 + TargetingAllOpponents）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Incinerate : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 2;
	// 卡牌类型（攻击）。
	private const CardType CardKind = CardType.Attack;
	// 卡牌稀有度（蓝卡 = Uncommon）。
	private const CardRarity CardRarityValue = CardRarity.Uncommon;
	// 目标类型（全体敌人）。
	private const TargetType CardTarget = TargetType.AllEnemies;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public Incinerate()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Incinerate.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：全体伤害 21（绑定 {Damage:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(21m, ValueProp.Move)
	];

	// 打出时：先消耗手牌中所有未附魔的卡牌，再对所有敌人造成伤害。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Attack", Owner.Character.AttackAnimDelay);

		// 1. 消耗手牌中所有未附魔的卡牌（附魔牌保留）。
		List<CardModel> unenchanted = Owner.PlayerCombatState!.Hand.Cards
			.Where(card => card.Enchantment == null)
			.ToList();
		foreach (CardModel card in unenchanted)
		{
			await CardCmd.Exhaust(choiceContext, card);
		}

		// 2. 对所有敌人造成伤害（参考硬着陆 CrashLanding 的 TargetingAllOpponents）。
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.TargetingAllOpponents(CombatState!)
			.Execute(choiceContext);
	}

	// 升级：伤害 21 -> 26。
	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(5m);
	}
}
