using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 术式-破袭（衍生减益符）：0 费衍生牌（类似小刀），打出后消耗；造成 3 点伤害并施加 2 层易伤。升级后伤害 4、易伤 3。
// 注册进原版 TokenCardPool（与小刀 Shiv 同类），因此不会出现在奖励/商店/图鉴等获取途径中。
[RegisterCard(typeof(TokenCardPool))]
public sealed class ArtOfBreach : ModCardTemplate
{
	// 基础耗能（衍生牌为 0 费）。
	private const int BaseEnergyCost = 0;
	// 卡牌类型（造成伤害的攻击牌）。
	private const CardType CardKind = CardType.Attack;
	// Token 稀有度：不参与奖励稀有度骰子。
	private const CardRarity CardRarityValue = CardRarity.Token;
	// 目标类型（任意敌人）。
	private const TargetType CardTarget = TargetType.AnyEnemy;
	// 衍生牌不出现在卡牌图鉴中。
	private const bool ShowInCardLibrary = false;

	public ArtOfBreach()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/ArtOfBreach.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 打出后自动消耗。
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

	// 基础数值：伤害 + 易伤层数。占位符 {VulnerablePower:diff()} 与 PowerVar<VulnerablePower> 绑定。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(3m, ValueProp.Move),
		new PowerVar<VulnerablePower>(2m)
	];

	// 打出时：先造成伤害，再施加易伤。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target);

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.Execute(choiceContext);

		await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target, DynamicVars.Vulnerable.BaseValue, Owner.Creature, this);
	}

	// 升级：伤害 3 -> 4，易伤 2 -> 3。
	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(1m);
		DynamicVars.Vulnerable.UpgradeValueBy(1m);
	}
}
