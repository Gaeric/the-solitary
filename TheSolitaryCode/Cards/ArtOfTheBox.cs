using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using TheSolitary.ArtEffects;

namespace TheSolitary.Cards;

// 匣中术（character.org 基础卡 #1）：1 费攻击。
// 造成3点伤害，随机触发2次术式；升级后触发术式+。
[RegisterCard(typeof(TheSolitaryCardPool))]
[RegisterCharacterStarterCard(typeof(TheSolitaryCharacter), 1, Order = 1)]
public sealed class ArtOfTheBox : ModCardTemplate
{
	protected override IEnumerable<IHoverTip> AdditionalHoverTips => ArtHoverTips.Create(IsUpgraded);

	private const int BaseEnergyCost = 1;
	private const CardType CardKind = CardType.Attack;
	// 卡牌稀有度（基础牌 = 初始卡，不出现在奖励/商店列表中）。
	private const CardRarity CardRarityValue = CardRarity.Basic;
	private const TargetType CardTarget = TargetType.AnyEnemy;
	private const bool ShowInCardLibrary = true;

	public ArtOfTheBox()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：伤害 + 术式触发次数（绑定 {Damage:diff()} / {Cards:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(3m, ValueProp.Move),
		new CardsVar(2)
	];

	// 打出时：先造成攻击伤害，再循环随机触发术式。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target);

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.Execute(choiceContext);

		for (int i = 0; i < DynamicVars.Cards.IntValue; i++)
		{
			await ArtTrigger.TriggerRandom(Owner, CombatState!, Owner.RunState.Rng.CombatCardGeneration, choiceContext, upgraded: IsUpgraded);
			await Cmd.Wait(0.1f);
		}
	}
}
