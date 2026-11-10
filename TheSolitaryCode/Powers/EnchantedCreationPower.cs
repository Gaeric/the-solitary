using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TheSolitary.ArtEffects;
using TheSolitary.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using TheSolitary.Enchantments;

namespace TheSolitary.Powers;

// 万物通元：每个打出的副本独立累计5次术式，所有手牌数值型附魔+1，仅本场战斗。
[RegisterPower]
public sealed class EnchantedCreationPower : ModPowerTemplate, IArtTriggerListener
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

	public Task AfterArtTriggered(PlayerChoiceContext choiceContext, ArtTriggerResult trigger)
	{
		if (trigger.Owner.Creature != Owner)
		{
			return Task.CompletedTask;
		}
		DynamicVar left = DynamicVars[TriggersLeftKey];
		left.BaseValue--;
		InvokeDisplayAmountChanged();
		if (left.IntValue > 0)
		{
			return Task.CompletedTask;
		}
		left.BaseValue = TriggerThreshold;
		InvokeDisplayAmountChanged();
		Flash();
		foreach (var card in trigger.Owner.PlayerCombatState!.Hand.Cards.ToList())
		{
			EnchantHelpers.IncreaseEnchantmentValue(card, amount: 1, persistToDeckVersion: false);
		}
		return Task.CompletedTask;
	}
}
