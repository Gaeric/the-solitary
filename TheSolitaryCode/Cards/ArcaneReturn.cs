using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TheSolitary.Characters;
using TheSolitary.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 术法归元（character.org 金卡 #14）：3 费能力牌。
// 每当你打出一张附魔牌时，生成一张随机术式并自动打出（目标为随机敌方）。
// 升级后不改变耗能，改由 Power 在触发时生成升级版术式（术式+）：
// ArcaneReturnPower.AfterApplied 捕获来源卡，按 IsUpgraded 决定生成术式还是术式+。
// 实现参考原版模仿学习 ImitationLearning（Power 覆写 AfterCardPlayed 钩子 + CardCmd.AutoPlay 随机目标自动打出），
// 结构与术式回想 SpellRecall（能力牌 + Power 钩子）保持一致。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class ArcaneReturn : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 3;
	// 卡牌类型（能力）。
	private const CardType CardKind = CardType.Power;
	// 卡牌稀有度（金卡 = Rare）。
	private const CardRarity CardRarityValue = CardRarity.Rare;
	// 目标类型（自身）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public ArcaneReturn()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/ArcaneReturn.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 打出时：播放 Power 施放动画，并给自己叠一层 ArcaneReturnPower。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
		await PowerCmd.Apply<ArcaneReturnPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
	}
}
