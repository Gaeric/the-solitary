using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Powers;

// 辐照（Irradiation）的 Power（蓝卡 辐照 授予的常驻效果）：
// 每当拥有者打出一张附魔牌，对所有敌人造成 Amount 点伤害。
// 附魔牌判定参考光栅 RasterPower.AfterCardPlayed；
// "所有敌人"伤害参考原版速行者 SpeedsterPower（CreatureCmd.Damage 攻击全体敌人）。
[RegisterPower]
public sealed class IrradiationPower : ModPowerTemplate
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	// 自定义图标（取材 WatcherBeautified 观者 Power 图标，128x128，小图与大图共用）。
	public override PowerAssetProfile AssetProfile => new(
		IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
		BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png");

	// 每当拥有者打出一张附魔牌时触发，对所有可命中敌人造成 Amount 点伤害。
	// 伤害来自 Power，使用 ValueProp.Unpowered（不享受力量/敏捷），
	// 全体敌人选取与伤害调用参考原版速行者 SpeedsterPower。
	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner != base.Owner.Player || cardPlay.Card.Enchantment == null)
		{
			return;
		}

		Flash();
		await CreatureCmd.Damage(choiceContext, base.Owner.CombatState!.HittableEnemies, base.Amount, ValueProp.Unpowered, base.Owner);
	}
}
