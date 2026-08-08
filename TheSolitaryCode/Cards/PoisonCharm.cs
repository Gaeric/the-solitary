using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 减益符（浸毒）：0 费衍生牌（类似小刀），打出后消耗；造成 2 点伤害并施加等量（2 层）中毒。
// RegisterCard 让 RitsuLib 注册这张牌；Token 稀有度 + ShowInCardLibrary=false 使它不出现在奖励/商店/图鉴中。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class PoisonCharm : ModCardTemplate
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

	public PoisonCharm()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 打出后自动消耗。
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

	// 基础数值：伤害 + 中毒层数（等量）。占位符 {PoisonPower:diff()} 与 PowerVar<PoisonPower> 绑定。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(2m, ValueProp.Move),
		new PowerVar<PoisonPower>(2m)
	];

	// 打出时：先造成伤害，再施加等量中毒。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target);

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.Execute(choiceContext);

		await PowerCmd.Apply<PoisonPower>(choiceContext, cardPlay.Target, DynamicVars.Poison.BaseValue, Owner.Creature, this);
	}
}
