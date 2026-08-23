using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 匣中术（character.org 基础卡 #1）：1 费攻击。
// 造成 3 点伤害，将 2 张随机术式加入手牌；升级后伤害 5，生成的术式变为升级版（术式+）。
[RegisterCard(typeof(TheSolitaryCardPool))]
[RegisterCharacterStarterCard(typeof(TheSolitaryCharacter), 1, Order = 1)]
public sealed class ArtOfTheBox : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 1;
	// 卡牌类型（攻击）。
	private const CardType CardKind = CardType.Attack;
	// 卡牌稀有度（基础牌 = 初始卡，不出现在奖励/商店列表中）。
	private const CardRarity CardRarityValue = CardRarity.Basic;
	// 目标类型（任意敌人）。
	private const TargetType CardTarget = TargetType.AnyEnemy;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public ArtOfTheBox()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：伤害 + 生成的术式数量（绑定 {Damage:diff()} / {Cards:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new DamageVar(3m, ValueProp.Move),
		new CardsVar(2)
	];

	// 打出时：先造成伤害，再循环生成随机术式加入手牌（本卡升级后生成升级版术式）。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target);

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.Execute(choiceContext);

		for (int i = 0; i < DynamicVars.Cards.IntValue; i++)
		{
			await Arts.CreateRandomInHand(Owner, CombatState!, Owner.RunState.Rng.CombatCardGeneration, choiceContext, upgraded: IsUpgraded);
			await Cmd.Wait(0.1f);
		}
	}

	// 升级后：伤害 3 -> 5（生成的术式数量不变，但变为升级版，由 IsUpgraded 控制）。
	protected override void OnUpgrade()
	{
		DynamicVars.Damage.UpgradeValueBy(2);
	}
}
