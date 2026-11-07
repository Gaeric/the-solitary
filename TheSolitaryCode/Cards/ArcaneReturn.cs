using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TheSolitary.Characters;
using TheSolitary.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Cards;

// 全知形态（原术法归元，character.org 金卡 #14）：3 费能力牌。
// 每当你打出一张附魔牌时，随机触发一次术式+。
// 升级后本卡获得保留，术式始终为升级版；Power 监听真实打牌事件。
[RegisterCard(typeof(TheSolitaryCardPool))]
public sealed class ArcaneReturn : ModCardTemplate
{
	private const int BaseEnergyCost = 3;
	private const CardType CardKind = CardType.Power;
	private const CardRarity CardRarityValue = CardRarity.Rare;
	private const TargetType CardTarget = TargetType.Self;
	private const bool ShowInCardLibrary = true;

	public ArcaneReturn()
		: base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
	{
	}

	public override CardAssetProfile AssetProfile => new(
		PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

	// 打出时：播放 Power 施放动画，并给自己叠一层 ArcaneReturnPower。
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
		await PowerCmd.Apply<ArcaneReturnPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
	}

	// 升级：本卡获得保留（Retain，回合结束不丢弃、留在手牌）。
	// 生成的术式不受升级影响（始终为术式+），故升级逻辑仅添加关键字。
	protected override void OnUpgrade()
	{
		AddKeyword(CardKeyword.Retain);
	}
}
