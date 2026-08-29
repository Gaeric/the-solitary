using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Powers;

// 余弦（Cosine）的 Power（蓝卡 余弦 的能力牌效果）：
// 每当拥有者生成一张术式时，获得 Amount 点格挡。
// 生成术式钩子参考术式回想 SpellRecallPower.AfterCardGeneratedForCombat（配合 Arts.IsArt 判定）。
[RegisterPower]
public sealed class CosinePower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	// 生成钩子：只处理拥有者自己生成的术式，为其发放格挡。
	public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
	{
		if (creator == null || creator.Creature != base.Owner || !Arts.IsArt(card))
		{
			return;
		}

		Flash();
		await CreatureCmd.GainBlock(base.Owner, base.Amount, ValueProp.Unpowered, null);
	}
}
