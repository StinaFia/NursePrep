namespace NursingQuiz.Core
{
    /// <summary>
    /// Which playable game mode the player picked on the main menu. Used by GameFlowController
    /// to route the shared theme+difficulty selection screen to the correct game screen/view.
    /// </summary>
    public enum GameModeType
    {
        MultipleChoice,
        MatchPairs
    }
}
