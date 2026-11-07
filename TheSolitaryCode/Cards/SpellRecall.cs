using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TheSolitary.Characters;
using TheSolitary.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 术式回想（新卡）：2 费能力牌，升级后 1 费。
// 所有术式效果额外触发一次；由独立 ArtTrigger 结算，不使用卡牌重放。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class SpellRecall : ModCardTemplate
{
	private const int BaseEnergyCost = 2;
	private const CardType CardKind = CardType.Power;
	private const CardRarity CardRarityValue = CardRarity.Rare;
	private const TargetType CardTarget = TargetType.Self;
	private const bool ShowInCardLibrary = true;

	public SpellRecall()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 打出时：播放 Power 施放动画，并给自己叠一层 SpellRecallPower。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
		await PowerCmd.Apply<SpellRecallPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
	}

	// 升级：费用 2 -> 1。
	protected override void OnUpgrade()
	{
		base.EnergyCost.UpgradeBy(-1);
	}
}
