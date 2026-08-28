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

// 光子映射（character.org 白卡，英文名 Photon Mapping）：2 费攻击。
// 随机对敌人造成 3 点伤害 4 次（升级后 5 次）；每当你洗牌 1 次，这张牌耗能减少 1（本场战斗内可叠加，战斗结束自动失效）。
// 随机敌人多段伤害参考原版 飞剑回旋镖 SwordBoomerang（RepeatVar + TargetingRandomOpponents）；
// 洗牌减费参考湍流 Turbulence（AfterShuffle 钩子 + EnergyCost.AddThisCombat(-1)）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class PhotonMapping : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 2;
	// 卡牌类型（攻击）。
	private const CardType CardKind = CardType.Attack;
	// 卡牌稀有度（白卡 = Common）。
	private const CardRarity CardRarityValue = CardRarity.Common;
	// 目标类型（随机敌人，无需手动选敌）。
	private const TargetType CardTarget = TargetType.RandomEnemy;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public PhotonMapping()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/PhotonMapping.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：单次伤害 3 + 命中次数 4（升级后 5），绑定 {Damage:diff()} / {Repeat:diff()} 占位符。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(3m, ValueProp.Move),
		new RepeatVar(4)
	];

	// 抽牌堆被洗牌时触发：本场战斗内这张牌耗能 -1（多次洗牌可叠加，reduceOnly 保证只减不增）。
	public override Task AfterShuffle(PlayerChoiceContext choiceContext, Player shuffler)
	{
		if (shuffler == Owner)
		{
			EnergyCost.AddThisCombat(-1, reduceOnly: true);
		}
		return Task.CompletedTask;
	}

	// 打出时：随机对敌人造成 3 点伤害，共 4 次（升级后 5 次，参考飞剑回旋镖的随机目标多段攻击）。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.WithHitCount(DynamicVars.Repeat.IntValue)
			.FromCard(this, cardPlay)
			.TargetingRandomOpponents(CombatState!)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);
	}

	// 升级：命中次数 4 -> 5（伤害与耗能保持不变）。
	protected override void OnUpgrade()
	{
		DynamicVars.Repeat.UpgradeValueBy(1m);
	}
}
