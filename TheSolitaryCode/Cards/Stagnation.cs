using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 凝滞（character.org 蓝卡 #13）：1 费技能，消耗。
// 给予所有敌方 1 层缓慢（升级后 2 层）。
// 全体施加参考原版 Scare / NegativePulse：TargetType.AllEnemies + 遍历 CombatState.HittableEnemies。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Stagnation : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（蓝卡 = Uncommon）。
	private const CardRarity CardRarityValue = CardRarity.Uncommon;
	// 目标类型（全体敌人，无手动选敌）。
	private const TargetType CardTarget = TargetType.AllEnemies;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public Stagnation()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Stagnation.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 打出后消耗。
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

	// 悬停提示：展示缓慢的机制说明。
	// 注意：RitsuLib 的 ModCardTemplate 已把 ExtraHoverTips sealed，悬停提示统一覆写 AdditionalHoverTips。
	protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<SlowPower>()];

	// 基础数值：缓慢层数（绑定 {SlowPower:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<SlowPower>(1m)
	];

	// 打出时：给所有可命中敌人施加缓慢（参考原版 Scare 的全体施加模式）。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

		// DynamicVarSet 没有 Slow 访问器，用索引器按 PowerVar 键名读取（升级后数值自动更新）。
		decimal slowAmount = DynamicVars["SlowPower"].BaseValue;
		foreach (Creature hittableEnemy in CombatState!.HittableEnemies)
		{
			await PowerCmd.Apply<SlowPower>(choiceContext, hittableEnemy, slowAmount, Owner.Creature, this);
		}
	}

	// 升级：缓慢层数 1 -> 2。
	protected override void OnUpgrade()
	{
		DynamicVars["SlowPower"].UpgradeValueBy(1m);
	}
}
