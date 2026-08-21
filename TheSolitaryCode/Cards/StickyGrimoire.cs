using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TheSolitary.Characters;
using TheSolitary.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 黏糊魔典（character.org anch #2，先古卡）：2 费能力牌。
// 战斗结束时，从牌组中选择一张牌附魔黏糊（Goopy：获得消耗；每打出一次，格挡值永久 +1）。
// 结构参考禁忌魔典 ForbiddenGrimoire（Ancient 能力牌 + Eternal 关键词 + 升级减费）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class StickyGrimoire : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 2;
	// 卡牌类型（能力）。
	private const CardType CardKind = CardType.Power;
	// 卡牌稀有度（先古 = Ancient）。
	private const CardRarity CardRarityValue = CardRarity.Ancient;
	// 目标类型（自身）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public StickyGrimoire()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 永恒：不可从牌组移除（参考禁忌魔典）。
	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Eternal];

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/StickyGrimoire.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 打出时：播放 Power 施放动画，并给自己叠一层 StickyGrimoirePower。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
		await PowerCmd.Apply<StickyGrimoirePower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
	}

	// 升级：费用 2 -> 1。
	protected override void OnUpgrade()
	{
		base.EnergyCost.UpgradeBy(-1);
	}
}
