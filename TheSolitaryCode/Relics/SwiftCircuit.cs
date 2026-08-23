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

// RegisterRelic 会把遗物注册进指定遗物池。
// RegisterCharacterStarterRelic 会把它作为 TheSolitaryCharacter 的初始遗物。
[RegisterRelic(typeof(TheSolitaryRelicPool))]
[RegisterCharacterStarterRelic(typeof(TheSolitaryCharacter))]
public sealed class SwiftCircuit : ModRelicTemplate
{
    // 每场战斗开始时重置的附魔次数（给抽到的前 N 张牌附魔迅捷）。
    private const int StartingCharges = 3;

    // 稀有度。
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(StartingCharges)
    ];

    // 图片资源统一放在 AssetProfile 里配置。
    // 三个路径可以先指向同一张图。后续有高清图或轮廓图时再拆开。
    public override RelicAssetProfile AssetProfile => new(
        // 小图标（原版 85x85）。
        IconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        // 轮廓图标（原版 85x85）。
        IconOutlinePath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        // 大图标（原版 256x256）。
        BigIconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png");

    // 遗物实例跨整局运行存在，充能计数只在 AfterCardDrawn 里递减、从不恢复，
    // 导致第一场战斗用完后就永远不再触发。游戏在每场战斗开始、回合初始抽牌之前
    // 派发 BeforeCombatStart 给全场模型，这里把充能重置回满。
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
            CardCmd.Enchant<Swift>(card, 1m);
            base.DynamicVars.Cards.BaseValue--;
        }
    }
}