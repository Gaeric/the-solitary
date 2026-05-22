
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;


namespace TheSolitary.Scripts;

[HarmonyPatch(typeof(UpMySleeve), MethodType.Constructor)]
public static class UpMySleeveConstructPatch
{
    private static void Postfix(Anticipate __instance)
    {
        ((DynamicVar)((CardModel)__instance).DynamicVars.Cards).BaseValue = 1m;
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
        LocString locStr = new LocString("cards","UP_MY_SLEEVE.selectionScreenPrompt");
        await CreatureCmd.TriggerAnim(((CardModel)card).Owner.Creature, "Cast", ((CardModel)card).Owner.Character.CastAnimDelay);
        List<CardModel> list = (await CardSelectCmd.FromHand(prefs: new CardSelectorPrefs(locStr, 0,
            ((CardModel)card).DynamicVars.Cards.IntValue), context: ctx, player: ((CardModel)card).Owner, filter: null, source: ((CardModel)card))).ToList();
        foreach (CardModel item in list)
        {
            CardModel cardModel = ((CardModel)card).CombatState.CreateCard<Shiv>(((CardModel)card).Owner);
            await CardCmd.Transform(item, cardModel);
        }
        ((CardModel)card).EnergyCost.AddThisCombat(-1);
    }
}
