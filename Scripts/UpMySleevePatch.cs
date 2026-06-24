
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;


namespace TheSolitary.Scripts;

[HarmonyPatch(typeof(UpMySleeve), "ExtraHoverTips", MethodType.Getter)]
public static class UpMySleeveHoverPatch
{
    [HarmonyPrefix]
    public static bool Prefix(ref IEnumerable<IHoverTip> __result)
    {
        List<IHoverTip> list = new List<IHoverTip>();
        list.Add(HoverTipFactory.FromCard<Shiv>());
        list.AddRange(HoverTipFactory.FromEnchantment<Swift>());
        __result = list;
        return false;
    }
}


[HarmonyPatch(typeof(UpMySleeve), MethodType.Constructor)]
public static class UpMySleeveConstructPatch
{
    private static void Postfix(UpMySleeve __instance)
    {
        ((DynamicVar)((CardModel)__instance).DynamicVars.Cards).BaseValue = 2m;
    }
}

// [HarmonyPatch(typeof(UpMySleeve), "OnUpgrade")]
public static class UpMySleeveOnUpgradePacth
{
    private static bool Prefix(UpMySleeve __instance)
    {
        ((CardModel)__instance).EnergyCost.UpgradeBy(-1);
        return false;
    }
}

[HarmonyPatch(typeof(UpMySleeve), MethodType.Constructor)]
public static class UpMySleeveRatityPacth
{
    private static readonly FieldInfo RarityField =
        typeof(CardModel).GetField("<Rarity>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static void Postfix(Acrobatics __instance)
    {
        if (RarityField != null)
        {
            RarityField.SetValue(__instance, (CardRarity)2);
        }
    }
}

[HarmonyPatch(typeof(UpMySleeve), "OnPlay")]
public static class UpMySleeveOnPlayPatch
{
    private static bool Prefix(UpMySleeve __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
    {
        __result = CustomPlay(__instance, choiceContext, cardPlay);
        return false;
    }

    private static async Task CustomPlay(UpMySleeve card, PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        var locStr = new LocString("cards","UP_MY_SLEEVE.selectionScreenPrompt");
        await CreatureCmd.TriggerAnim(((CardModel)card).Owner.Creature, "Cast", ((CardModel)card).Owner.Character.CastAnimDelay);
        var list = (await CardSelectCmd.FromHand(prefs: new CardSelectorPrefs(locStr, 0,
            ((CardModel)card).DynamicVars.Cards.IntValue), context: ctx, player: ((CardModel)card).Owner, filter: null, source: ((CardModel)card))).ToList();
        foreach (var item in list)
        {
            await CardCmd.Exhaust(ctx, item);
            // CardModel cardModel = ((CardModel)card).CombatState!.CreateCard<Shiv>(((CardModel)card).Owner);
            // CardCmd.Enchant<Swift>(cardModel, 1m);
            // await CardCmd.Transform(item, cardModel);
        }
        foreach (var item in await Shiv.CreateInHand(((CardModel)card).Owner, ((CardModel)card).DynamicVars.Cards.IntValue, ((CardModel)card).CombatState!))
        {
            CardCmd.Enchant<Swift>(item, 1m);
        }


        ((CardModel)card).EnergyCost.AddThisCombat(-1);
    }
}
