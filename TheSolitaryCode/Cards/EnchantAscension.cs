using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TheSolitary.Characters;
using TheSolitary.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 凝华：打出后，本场战斗结束时获得一次牌组随机附魔选牌奖励。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class EnchantAscension : ModCardTemplate
{
	private const int BaseEnergyCost = 1;
	private const CardType CardKind = CardType.Power;
	private const CardRarity CardRarityValue = CardRarity.Rare;
	private const TargetType CardTarget = TargetType.Self;
	private const bool ShowInCardLibrary = true;

	public EnchantAscension()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 只在打出后生效，不能通过牌组里的 AfterCombatEnd 被动获取奖励。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
		await PowerCmd.Apply<EnchantAscensionPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
	}

	// 升级：费用 1 -> 0。
	protected override void OnUpgrade()
	{
		base.EnergyCost.UpgradeBy(-1);
	}
}
