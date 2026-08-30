using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 风暴（character.org 蓝卡 #8）：1 费攻击。
// 造成 8 点伤害，你每有一张附魔牌，造成额外 4 点伤害。
// 计算结构参考灰烬打击 AshenStrike；附魔牌数量统计与共轭 Conjugate 共用 EnchantHelpers。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class EnchantStorm : ModCardTemplate
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

	public EnchantStorm()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/EnchantStorm.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：计算伤害 = 基础(8) + 附魔牌数量 × ExtraDamage(4)。
	// 绑定 {CalculatedDamage:diff()} / {ExtraDamage:diff()} 占位符（参考原版 AshenStrike 的结构）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CalculationBaseVar(8m),
		new ExtraDamageVar(3m),
		new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (card, _) =>
			EnchantHelpers.CountEnchantedCardsInAllPiles(card.Owner))
	];

	// 打出时：按计算出的总伤害攻击目标。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target);

		await DamageCmd.Attack(DynamicVars.CalculatedDamage)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.Execute(choiceContext);
	}

	// 升级：每张附魔牌造成的额外伤害 4 -> 6。
	protected override void OnUpgrade()
	{
		DynamicVars.ExtraDamage.UpgradeValueBy(1m);
	}
}
