using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 撒符（参考袖里乾坤 UpMySleeve）：2 费 Skill。
// 将 3 张（升级后 4 张）随机减益符加入手牌；本场战斗每打出一次，本卡费用 -1。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class ScatterCharms : ModCardTemplate
{
	// 基础耗能。
	private const int BaseEnergyCost = 2;
	// 卡牌类型（Skill）。
	private const CardType CardKind = CardType.Skill;
	// 卡牌稀有度（蓝卡 = Uncommon）。
	private const CardRarity CardRarityValue = CardRarity.Uncommon;
	// 目标类型（Self）。
	private const TargetType CardTarget = TargetType.Self;
	// 是否在卡牌图鉴中显示。
	private const bool ShowInCardLibrary = true;

	// 本场战斗已打出的次数（参考 UpMySleeve 的计数）。
	private int _timesPlayedThisCombat;

	private int TimesPlayedThisCombat
	{
		get
		{
			return _timesPlayedThisCombat;
		}
		set
		{
			AssertMutable();
			_timesPlayedThisCombat = value;
		}
	}

	public ScatterCharms()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	// 卡图资源；文件名与类名一致（TheSolitary/images/cards/）。
	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：生成的减益符数量（绑定 {Cards:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(3)
	];

	// 打出时：循环生成随机减益符加入手牌；随后本场战斗费用 -1（参考 UpMySleeve）。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		for (int i = 0; i < DynamicVars.Cards.IntValue; i++)
		{
			await DebuffCharms.CreateRandomInHand(Owner, CombatState!, Owner.RunState.Rng.CombatCardGeneration, choiceContext);
			await Cmd.Wait(0.1f);
		}

		TimesPlayedThisCombat++;
		EnergyCost.AddThisCombat(-1);
	}

	// 升级后：多生成一张（3 -> 4）。
	protected override void OnUpgrade()
	{
		DynamicVars.Cards.UpgradeValueBy(1);
	}
}
