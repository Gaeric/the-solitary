using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using TheSolitary.ArtEffects;

namespace TheSolitary.Powers;

[RegisterPower]
public sealed class ArcaneWeavingPower : ModPowerTemplate, IArtTriggerListener
{
	protected override IEnumerable<IHoverTip> AdditionalHoverTips => ArtHoverTips.Create();

	private const string TriggersLeftKey = "TriggersLeft";
	private const int TriggerThreshold = 5;
	public override PowerType Type => PowerType.Buff;
	public override PowerStackType StackType => PowerStackType.Counter;
	public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
	public override int DisplayAmount => DynamicVars[TriggersLeftKey].IntValue;
	protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(TriggersLeftKey, TriggerThreshold)];
	public override PowerAssetProfile AssetProfile => new(
		IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
		BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png");

	public async Task AfterArtTriggered(PlayerChoiceContext choiceContext, ArtTriggerResult trigger)
	{
		if (trigger.Owner.Creature != Owner)
		{
			return;
		}
		DynamicVar left = DynamicVars[TriggersLeftKey];
		left.BaseValue--;
		InvokeDisplayAmountChanged();
		if (left.IntValue > 0)
		{
			return;
		}
		left.BaseValue = TriggerThreshold;
		InvokeDisplayAmountChanged();
		// 致死的术式仍会收到通知，但战斗结束后不再往战斗牌堆生成卡牌。
		if (CombatManager.Instance.IsOverOrEnding)
		{
			return;
		}
		var player = trigger.Owner;
		var card = CardFactory.GetDistinctForCombat(player,
			ModelDb.CardPool<ColorlessCardPool>().GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint),
			1, player.RunState.Rng.CombatCardGeneration).FirstOrDefault();
		if (card != null)
		{
			Flash();
			await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
		}
	}
}