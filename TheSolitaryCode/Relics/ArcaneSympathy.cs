using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TheSolitary.Cards;
using TheSolitary.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace TheSolitary.Relics;

// 术元交感（稀有遗物）：你每打出 4 张术式，为手牌中的一张牌随机附魔。
// 计数跨战斗累计（"每打出 N 张"语义，参考原版念珠 Nunchaku 的持久计数 + 模运算展示）。
[RegisterRelic(typeof(TheSolitaryRelicPool))]
public sealed class ArcaneSympathy : ModRelicTemplate
{
    // 触发阈值：每打出几张术式结算一次。
    private const int ArtsThreshold = 4;

    // 自上次触发以来打出的术式总数（跨战斗累计，用模运算判定触发）。
    private int _artsPlayed;

    public override RelicRarity Rarity => RelicRarity.Rare;

    // 遗物角标显示当前进度（距离下一次触发的剩余术式数）。
    public override bool ShowCounter => true;

    public override int DisplayAmount
    {
        get
        {
            int threshold = DynamicVars.Cards.IntValue;
            return ArtsPlayed % threshold;
        }
    }

    // 基础数值：阈值（绑定 {Cards:diff()} 占位符）。
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(ArtsThreshold)
    ];

    private int ArtsPlayed
    {
        get => _artsPlayed;
        set
        {
            AssertMutable();
            _artsPlayed = value;
            UpdateDisplay();
        }
    }

    // 图片资源统一放在 AssetProfile 里配置（三个路径先指向同一张图）。
    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/relics/{GetType().Name}.png");

    private void UpdateDisplay()
    {
        int threshold = DynamicVars.Cards.IntValue;
        base.Status = (ArtsPlayed % threshold == threshold - 1) ? RelicStatus.Active : RelicStatus.Normal;
        InvokeDisplayAmountChanged();
    }

    // 打出术式时累计；达到阈值后为手牌中的一张牌随机附魔（无可用目标则本次无事发生）。
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner || !Arts.IsArt(cardPlay.Card))
        {
            return;
        }

        ArtsPlayed++;
        int threshold = DynamicVars.Cards.IntValue;
        if (ArtsPlayed % threshold != 0)
        {
            return;
        }

        // 手牌中没有任何可被随机附魔池附魔的牌时跳过（不重置计数，保持"每 N 张结算一次"节奏）。
        List<CardModel> candidates = base.Owner.PlayerCombatState!.Hand.Cards
            .Where(RandomEnchantPool.CanEnchantRandomly)
            .ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        Flash();
        CardModel? target = base.Owner.RunState.Rng.CombatCardGeneration.NextItem(candidates);
        if (target != null)
        {
            RandomEnchantPool.EnchantRandomly(base.Owner.RunState.Rng.CombatCardGeneration, target);
        }
    }
}
