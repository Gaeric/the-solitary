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
using TheSolitary.Enchantments;

namespace TheSolitary.Relics;

[RegisterRelic(typeof(TheSolitaryRelicPool))]
[RegisterCharacterStarterRelic(typeof(TheSolitaryCharacter))]
public sealed class SwiftCircuit : ModRelicTemplate
{
    // 每场战斗开始时重置的附魔次数（给抽到的前 N 张牌附魔迅捷）。
    private const int StartingCharges = 3;

    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(StartingCharges)
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png");

    // 遗物跨战斗保留；每场战斗的初始抽牌前重置充能。
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
            && EnchantHelpers.CanEnchant<Swift>(card))
        {
            CardCmd.Enchant<Swift>(card, 1m);
            base.DynamicVars.Cards.BaseValue--;
        }
    }
}