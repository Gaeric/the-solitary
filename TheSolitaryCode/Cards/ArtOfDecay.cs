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

// 术式-凋零（衍生减益符）：0 费衍生牌（类似小刀），打出后消耗；造成 4 点伤害并施加 1 层缓慢。升级后伤害 5。
// 注册进原版 TokenCardPool（与小刀 Shiv 同类），因此不会出现在奖励/商店/图鉴等获取途径中。
[RegisterCard(typeof(TokenCardPool))]
public sealed class ArtOfDecay : ModCardTemplate
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

	public ArtOfDecay()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/ArtOfDecay.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 打出后自动消耗。
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

	// 基础数值：伤害 + 缓慢层数。占位符 {SlowPower:diff()} 与 PowerVar<SlowPower> 绑定。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(4m, ValueProp.Move),
		new PowerVar<SlowPower>(1m)
	];

	// 打出时：先造成伤害，再施加缓慢。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target);

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.Execute(choiceContext);

		// DynamicVarSet 没有 Slow 访问器，缓慢层数用索引器取值（与 CanonicalVars 中的 PowerVar 保持一致）。
		await PowerCmd.Apply<SlowPower>(choiceContext, cardPlay.Target, DynamicVars["SlowPower"].BaseValue, Owner.Creature, this);
	}

	// 升级：伤害 4 -> 5（缓慢层数不变）。
	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(1m);
	}
}
