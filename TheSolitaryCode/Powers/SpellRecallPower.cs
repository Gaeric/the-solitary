using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using TheSolitary.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Powers;

// 术式回想的 Power（参考附魔造物 EnchantedCreationPower 的 AfterCardGeneratedForCombat 钩子）：
// 每当拥有者生成一张减益符时，使其获得重放（Replay：打出时额外打一次，参考原版变形 Transfigure）。
[RegisterPower]
public sealed class SpellRecallPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	// 生成钩子：只处理拥有者自己生成的减益符，为其增加重放。
	public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
	{
		if (creator == null || creator.Creature != base.Owner || !DebuffCharms.IsDebuffCharm(card))
		{
			return Task.CompletedTask;
		}

		Flash();
		card.BaseReplayCount++;
		return Task.CompletedTask;
	}
}
