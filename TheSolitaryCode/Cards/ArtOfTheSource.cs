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

// 能元妙术（参考无尽刀刃 InfiniteBlades）：1 费 Power。
// 回合开始时随机触发一次术式+。升级后获得固有。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class ArtOfTheSource : ModCardTemplate
{
	protected override IEnumerable<IHoverTip> AdditionalHoverTips => ArtHoverTips.Create(upgraded: true);

	private const int BaseEnergyCost = 1;
	private const CardType CardKind = CardType.Power;
	private const CardRarity CardRarityValue = CardRarity.Uncommon;
	private const TargetType CardTarget = TargetType.Self;
	private const bool ShowInCardLibrary = true;

	public ArtOfTheSource()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 打出时：播放 Power 施放动画，并给自己叠一层 ArtOfTheSourcePower。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
		await PowerCmd.Apply<ArtOfTheSourcePower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
	}

	// 升级后获得固有（战斗开始时在手牌中）。
	protected override void OnUpgrade()
	{
		AddKeyword(CardKeyword.Innate);
	}
}
