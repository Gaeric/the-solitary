using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TheSolitary.Cards;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Relics;

// 来回往复（罕见遗物）：每当你洗牌时，获得一张随机术式。
[RegisterRelic(typeof(TheSolitaryRelicPool))]
public sealed class BackAndForth : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    // 图片资源统一放在 AssetProfile 里配置（三个路径先指向同一张图）。
    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png");

    // 洗牌时获得一张随机术式（与环回形态 LoopFormPower 同款 AfterShuffle 钩子：
    // 先校验触发者归属，再执行效果；生成用 Arts.CreateRandomInHand，随机数用战斗抽牌 RNG）。
    public override async Task AfterShuffle(PlayerChoiceContext choiceContext, Player shuffler)
    {
        if (shuffler != base.Owner)
        {
            return;
        }

        Flash();
        await Arts.CreateRandomInHand(
            base.Owner,
            base.Owner.Creature.CombatState!,
            base.Owner.RunState.Rng.CombatCardGeneration,
            choiceContext);
    }
}
