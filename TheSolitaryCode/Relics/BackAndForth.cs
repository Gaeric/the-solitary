using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using TheSolitary.Cards;
using TheSolitary.ArtEffects;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Relics;

// 来回往复（罕见遗物）：每当你洗牌时，随机触发一次术式。
[RegisterRelic(typeof(TheSolitaryRelicPool))]
public sealed class BackAndForth : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    // 图片资源统一放在 AssetProfile 里配置（三个路径先指向同一张图）。
    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png");

    // 洗牌时触发术式；先校验洗牌者归属。
    public override async Task AfterShuffle(PlayerChoiceContext choiceContext, Player shuffler)
    {
        if (shuffler != base.Owner)
        {
            return;
        }

        Flash();
        await ArtTrigger.TriggerRandom(
            base.Owner,
            base.Owner.Creature.CombatState!,
            base.Owner.RunState.Rng.CombatCardGeneration,
            choiceContext);
    }
}
