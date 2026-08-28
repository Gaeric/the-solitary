using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using TheSolitary.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Powers;

// 万物通元的 Power（参考原版 PillarOfCreationPower / SmokestackPower 的 AfterCardGeneratedForCombat 钩子）：
// 每当拥有者生成一张牌时，为它随机附魔（随机附魔池与唤醒一致）。
// 注：轮回/元能转置直接在原卡上交换附魔（不再通过 Transform 重建卡牌），
// 不会触发生成牌钩子，因此不会对交换中的牌误附魔。
[RegisterPower]
public sealed class EnchantedCreationPower : ModPowerTemplate
{
	// 防止在附魔过程中再次触发“生成牌”钩子导致递归（参考灵魂绑定 SoulboundPower 的守卫模式）。
	private bool _isEnchanting;

	private bool IsEnchanting
	{
		get
		{
			return _isEnchanting;
		}
		set
		{
			AssertMutable();
			_isEnchanting = value;
		}
	}

	public override PowerType Type => PowerType.Buff;

	// 不计数值的二进制类型：效果是「每张生成的牌都被附魔」，叠多层没有额外效果，
	// 因此不显示层数（原版 Barricade / Corruption 同款 PowerStackType.Single）。
	public override PowerStackType StackType => PowerStackType.Single;

	// 只处理自己生成的牌；已附魔的牌与递归中的生成跳过。
	public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
	{
		if (creator == null || creator.Creature != base.Owner || card.Enchantment != null || IsEnchanting)
		{
			return Task.CompletedTask;
		}

		IsEnchanting = true;
		Flash();
		RandomEnchantPool.EnchantRandomly(base.Owner.Player!.RunState.Rng.CombatCardSelection, card);
		IsEnchanting = false;
		return Task.CompletedTask;
	}
}
