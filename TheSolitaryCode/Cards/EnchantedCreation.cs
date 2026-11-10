using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using TheSolitary.ArtEffects;
using TheSolitary.Characters;
using TheSolitary.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 万物通元：2费金色能力，每触发5次术式，所有手牌附魔数值+1（仅本场战斗）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class EnchantedCreation : ModCardTemplate
{
	protected override IEnumerable<IHoverTip> AdditionalHoverTips => ArtHoverTips.Create();

	private const int BaseEnergyCost = 2;
	private const CardType CardKind = CardType.Power;
	private const CardRarity CardRarityValue = CardRarity.Rare;
	private const TargetType CardTarget = TargetType.Self;
	private const bool ShowInCardLibrary = true;

	public EnchantedCreation()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 打出时：播放 Power 施放动画，并给自己叠一层 EnchantedCreationPower。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
		await PowerCmd.Apply<EnchantedCreationPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
	}

	// 升级：费用不变，获得固有。
	protected override void OnUpgrade()
	{
		AddKeyword(CardKeyword.Innate);
	}
}
