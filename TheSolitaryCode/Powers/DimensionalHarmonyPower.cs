using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using TheSolitary.Cards;

namespace TheSolitary.Powers;

[RegisterPower]
public sealed class DimensionalHarmonyPower : ModPowerTemplate
{
	private bool _isEnchanting;
	public override PowerType Type => PowerType.Buff;
	// 一张牌只能保有一种附魔；重复施加不重复替换附魔。
	public override PowerStackType StackType => PowerStackType.Single;
	public override PowerAssetProfile AssetProfile => new(
		IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
		BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png");

	public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
	{
		if (creator?.Creature != Owner || card.Owner != Owner.Player || card.Enchantment != null || _isEnchanting
			|| !RandomEnchantPool.CanEnchantRandomly(card))
		{
			return Task.CompletedTask;
		}
		_isEnchanting = true;
		try
		{
			Flash();
			RandomEnchantPool.EnchantRandomly(Owner.Player!.RunState.Rng.CombatCardSelection, card);
		}
		finally
		{
			_isEnchanting = false;
		}
		return Task.CompletedTask;
	}
}