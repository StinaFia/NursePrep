namespace NursingQuiz.Core
{
    /// <summary>
    /// Shared contract for every game mode (multiple choice, memory card, find the pairs, ...).
    /// The game shell (menu -> mode select -> difficulty select -> game -> results) talks to
    /// modes only through this interface, so new modes can be added later without touching the shell.
    /// </summary>
    public interface IGameMode
    {
        /// <summary>Prepare the mode with a chosen topic and difficulty. Called once before play starts.</summary>
        void Setup(string aihe, DifficultyTier tier, int questionCount);

        /// <summary>Start (or restart) a round/session.</summary>
        void StartRound();

        /// <summary>Called by the mode's own UI when the player submits an answer for the current item.</summary>
        void EvaluateAnswer(object answer);

        /// <summary>Called when the round ends (all items answered, or player quits).</summary>
        void EndSession();
    }
}
