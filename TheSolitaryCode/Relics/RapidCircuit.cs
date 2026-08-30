using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Relics;

// 极速回路：迅捷回路的先古精炼版（欧洛巴斯之触 TouchOfOrobas 事件的升级产物）。
// 与迅捷回路同款机制，但为抽到的前 N 张牌附魔的迅捷层数为 2。
// 映射在 Entry.Initialize 中通过 RitsuLibFramework.RegisterTouchOfOrobasRefinementMapping<SwiftCircuit, RapidCircuit>() 注册。
[RegisterRelic(typeof(TheSolitaryRelicPool))]
public sealed class RapidCircuit : ModRelicTemplate
{
    // 每场战斗开始时重置的附魔次数（给抽到的前 N 张牌附魔迅捷）。
    private const int StartingCharges = 3;

    // 每次附魔的迅捷层数（迅捷：本回合打出这张牌时获得等量能量）。
    private const decimal SwiftAmount = 2m;

    public override RelicRarity Rarity => RelicRarity.Starter;

    // 基础数值：充能次数 + 迅捷层数（{Cards} / {SwiftAmount} 占位符）。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(StartingCharges),
        new DynamicVar("SwiftAmount", SwiftAmount)
    ];

    // 图片资源统一放在 AssetProfile 里配置（三个路径先指向同一张图）。
    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png");

    // 遗物实例跨整局运行存在，充能计数只在 AfterCardDrawn 里递减、从不恢复，
    // 导致第一场战斗用完后就永远不再触发。每场战斗开始、回合初始抽牌之前重置回满。
    public override Task BeforeCombatStart()
    {
        base.DynamicVars.Cards.BaseValue = StartingCharges;
        return Task.CompletedTask;
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        // 只有能被迅捷附魔的卡牌（非状态/诅咒、未被其他附魔占用等）才添加迅捷并消耗充能。
        // CardCmd.Enchant 内部会再次调用 CanEnchant，若卡牌不可附魔会直接抛异常，因此必须先检查。
        if (card.Owner == base.Owner && base.DynamicVars.Cards.BaseValue > 0
            && ModelDb.Enchantment<Swift>().ToMutable().CanEnchant(card))
        {
            CardCmd.Enchant<Swift>(card, SwiftAmount);
            base.DynamicVars.Cards.BaseValue--;
        }
    }
}
