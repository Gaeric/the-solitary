using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheSolitary.Characters;
using TheSolitary.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 抽样（character.org todo 金卡）：1 费技能。
// 你打出的下一张附魔牌被打出两次（升级后下两张附魔牌被打出两次）。
// 实现参考原版 复制药水 DuplicationPower（ModifyCardPlayCount 加一次 + 次数递减）。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class Sampling : ModCardTemplate
{
	private const int BaseEnergyCost = 1;
	private const CardType CardKind = CardType.Skill;
	private const CardRarity CardRarityValue = CardRarity.Rare;
	private const TargetType CardTarget = TargetType.Self;
	private const bool ShowInCardLibrary = true;

	public Sampling()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 基础数值：被打出两次的附魔牌张数（绑定 {SamplingPower:diff()} 占位符）。
	protected override IEnumerable<DynamicVar> CanonicalVars =>
	[
		new PowerVar<SamplingPower>(1m)
	];

	// 打出时：授予一次性 SamplingPower（层数 = 被打出两次的附魔牌张数）。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
		await PowerCmd.Apply<SamplingPower>(choiceContext, Owner.Creature,
			DynamicVars["SamplingPower"].BaseValue, Owner.Creature, this);
	}

	// 升级：下一张 -> 下两张。
	protected override void OnUpgrade()
	{
		DynamicVars["SamplingPower"].UpgradeValueBy(1m);
	}
}
