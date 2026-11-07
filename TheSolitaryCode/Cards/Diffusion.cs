using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 扩散（character.org todo 白卡）：1 费攻击。
// 对所有敌人造成 8 点伤害（升级后 11 点）；如果这张卡自身带有附魔，额外抽 1 张牌。
// 全体攻击参考原版 旋风斩 Whirlwind（TargetingAllOpponents）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Diffusion : ModCardTemplate
{
	private const int BaseEnergyCost = 1;
	private const CardType CardKind = CardType.Attack;
	private const CardRarity CardRarityValue = CardRarity.Common;
	private const TargetType CardTarget = TargetType.AllEnemies;
	private const bool ShowInCardLibrary = true;

	public Diffusion()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：伤害 + 额外抽牌数（绑定 {Damage:diff()} / {Cards:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(8m, ValueProp.Move),
		new CardsVar(1)
	];

	// 打出时：对所有敌人造成伤害；如果这张卡自身带有附魔，额外抽 1 张牌。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.TargetingAllOpponents(CombatState!)
			.Execute(choiceContext);

		// “有附魔”指这张卡自身带有附魔（cardPlay.Card 即当前打出的卡牌实例）。
		if (cardPlay.Card.Enchantment != null)
		{
			await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
		}
	}

	// 升级：伤害 8 -> 11。
	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(3m);
	}
}
