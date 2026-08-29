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
// 每当拥有者生成一张术式时，使其获得重放（Replay：打出时额外打一次，参考原版变形 Transfigure）。
[RegisterPower]
public sealed class SpellRecallPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	// 自定义图标（取材 WatcherBeautified 观者 Power 图标，128x128，小图与大图共用）。
	public override PowerAssetProfile AssetProfile => new(
		IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
		BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png");

	// 生成钩子：只处理拥有者自己生成的术式，为其增加重放。
	public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
	{
		if (creator == null || creator.Creature != base.Owner || !Arts.IsArt(card))
		{
			return Task.CompletedTask;
		}

		Flash();
		card.BaseReplayCount++;
		return Task.CompletedTask;
	}
}
