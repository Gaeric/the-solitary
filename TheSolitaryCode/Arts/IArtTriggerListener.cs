using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TheSolitary.ArtEffects;

// 独立术式钩子：每次实际结算（包含回想的额外结算）通知一次。
public interface IArtTriggerListener
{
	Task AfterArtTriggered(PlayerChoiceContext choiceContext, ArtTriggerResult trigger);
}