using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TheSolitary.Cards;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Relics;

// 凝术御甲（普通遗物）：每打出一张术式牌，获得 1 点格挡。
[RegisterRelic(typeof(TheSolitaryRelicPool))]
public sealed class ArcaneAegis : ModRelicTemplate
{
    // 每打出一张术式获得的格挡。
    private const decimal BlockAmount = 1m;

    public override RelicRarity Rarity => RelicRarity.Common;

    // 基础数值：格挡（绑定 {Block:diff()} 占位符）。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(BlockAmount, ValueProp.Unpowered)
    ];

    // 图片资源统一放在 AssetProfile 里配置（三个路径先指向同一张图）。
    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png");

    // 打出术式时获得格挡（术式判定复用 Arts.IsArt，与余弦/术式回想同款）。
    // ValueProp.Unpowered：来自遗物的格挡不享受力量/敏捷加成（与原版 Orichalcum 等遗物一致）。
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner == base.Owner && Arts.IsArt(cardPlay.Card))
        {
            Flash();
            await CreatureCmd.GainBlock(base.Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Unpowered, null);
        }
    }
}
