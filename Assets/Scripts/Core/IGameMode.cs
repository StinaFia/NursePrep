namespace NursingQuiz.Core
{
    /// <summary>
    /// Common interface implemented by all game modes.
    ///
    /// GameFlowController interacts with game modes through this interface, allowing new modes to
    /// be added without changing the overall game flow.
    /// </summary>
    public interface IGameMode
    {
        /// <summary>
        /// Initializes the game mode using the selected category, difficulty tier and question count.
        /// Called once before the round begins.
        /// </summary>
        void Setup(string category, DifficultyTier tier, int questionCount);

        /// <summary>
        /// Starts a new round or restarts the current round.
        /// </summary>
        void StartRound();

        /// <summary>
        /// Evaluates a submitted answer.
        ///
        /// Different game modes may use different answer types, so the answer is provided as an object.
        /// </summary>
        void EvaluateAnswer(object answer);

        /// <summary>
        /// Ends the current session and performs anyrequired cleanup or result processing.
        /// </summary>        
        void EndSession();
    }
}
