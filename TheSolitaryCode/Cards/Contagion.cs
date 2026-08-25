using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
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

// 透射（character.org 金卡 #8）：1 费攻击。
// 造成 12 点伤害（升级后 16）；斩杀时，给予其他敌人该敌人身上的所有负面效果。
// 斩杀判定用 DamageResult.WasTargetKilled；减益转移用 PowerCmd.Apply（参考衍射 Aggravate 的枚举+施加方式）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Contagion : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（攻击）。
	private const CardType CardKind = CardType.Attack;
	// 卡牌稀有度（金卡 = Rare）。
	private const CardRarity CardRarityValue = CardRarity.Rare;
	// 目标类型（任意敌人）。
	private const TargetType CardTarget = TargetType.AnyEnemy;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public Contagion()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Contagion.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：伤害 12（升级后 16），绑定 {Damage:diff()} 占位符。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(12m, ValueProp.Move)
	];

	// 打出时：先快照目标身上的所有减益（类型+层数），再造成伤害；
	// 若本次攻击斩杀目标，把快照的每种减益按原层数给予其他存活敌人。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target);

		// 快照在造成伤害之前进行（敌人死亡后其 Power 可能被清理，无法再读取）。
		List<(Type Type, int Amount)> debuffs = cardPlay.Target.Powers
			.Where(p => p.Type == PowerType.Debuff)
			.Select(p => (p.GetType(), p.Amount))
			.ToList();

		AttackCommand attack = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.Execute(choiceContext);

		// 仅本次攻击击杀目标时转移（WasTargetKilled 精确标记本次伤害是否致死）。
		bool wasKilled = attack.Results.SelectMany(hit => hit).Any(r => r.WasTargetKilled);
		if (!wasKilled || debuffs.Count == 0)
		{
			return;
		}

		// 给予其他敌人（HittableEnemies 只含存活可命中敌人，再显式排除目标）。
		foreach (Creature other in CombatState!.HittableEnemies.Where(e => e != cardPlay.Target))
		{
			foreach ((Type debuffType, int amount) in debuffs)
			{
				await PowerCmd.Apply(choiceContext,
					ModelDb.DebugPower(debuffType).ToMutable(),
					other, amount, Owner.Creature, this);
			}
		}
	}

	// 升级：伤害 12 -> 16。
	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(4m);
	}
}
