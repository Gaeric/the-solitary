using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using TheSolitary.ArtEffects;

namespace TheSolitary.Cards;

// 掌中奇术（参考袖里乾坤 UpMySleeve）：1 费 Skill。
// 将 3 张随机术式加入手牌；本场战斗每打出一次，本卡费用 -1。
// 升级后生成的术式变为升级版（术式+），数量保持 3 张。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class ArtOfThePalm : ModCardTemplate
{
	private const int BaseEnergyCost = 1;
	private const CardType CardKind = CardType.Skill;
	private const CardRarity CardRarityValue = CardRarity.Common;
	private const TargetType CardTarget = TargetType.Self;
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

	public ArtOfThePalm()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：术式触发次数（绑定 {Cards:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new CardsVar(3)
	];

	// 打出时：循环生成随机术式加入手牌（本卡升级后生成升级版术式）；随后本场战斗费用 -1（参考 UpMySleeve）。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		for (int i = 0; i < DynamicVars.Cards.IntValue; i++)
		{
			await ArtTrigger.TriggerRandom(Owner, CombatState!, Owner.RunState.Rng.CombatCardGeneration, choiceContext, upgraded: IsUpgraded);
			await Cmd.Wait(0.1f);
		}

		TimesPlayedThisCombat++;
		EnergyCost.AddThisCombat(-1);
	}

	// 升级后：数量不变（仍为 3 张），升级效果由 OnPlay 中的 IsUpgraded 决定（生成术式+）。
	protected override void OnUpgrade()
	{
	}
}
