using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 湍流（character.org 白卡 #2）：2 费技能。
// 获得 5 点格挡（升级后 8），抽 2 张牌；每当你洗牌 1 次，这张牌耗能减少 1（本场战斗内可叠加，战斗结束自动失效）。
// 洗牌减费参考原版 王者之踢 KinglyKick：AfterShuffle 钩子 + EnergyCost.AddThisCombat(-1)。
// 升级：格挡 5 -> 8，基础耗能 2 -> 1。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Turbulence : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 2;
	// 卡牌类型（技能）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（白卡 = Common）。
	private const CardRarity CardRarityValue = CardRarity.Common;
	// 目标类型（自身）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	public Turbulence()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/Turbulence.png）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 获得格挡，按格挡牌参与力量/敏捷等计算。
	public override bool GainsBlock => true;

	// 基础数值：格挡 5（升级后 8）+ 抽牌数 2（绑定 {Block:diff()} / {Cards:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new BlockVar(5m, ValueProp.Move),
		new CardsVar(2)
	];

	// 抽牌堆被洗牌时触发：本场战斗内这张牌耗能 -1（多次洗牌可叠加，reduceOnly 保证只减不增）。
	public override Task AfterShuffle(PlayerChoiceContext choiceContext, Player shuffler)
	{
		if (shuffler == Owner)
		{
			EnergyCost.AddThisCombat(-1, reduceOnly: true);
		}
		return Task.CompletedTask;
	}

	// 打出时：获得格挡并抽 2 张牌。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
		await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
	}

	// 升级：格挡 5 -> 8，基础耗能 2 -> 1（洗牌减费机制不变）。
	protected override void OnUpgrade()
	{
		DynamicVars.Block.UpgradeValueBy(3m);
		EnergyCost.UpgradeBy(-1);
	}
}
