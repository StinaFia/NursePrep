using UnityEngine;

namespace NursingQuiz.Core
{
    /// <summary>
    /// Base class for game modes.
    ///
    /// Provides shared functionality such as:
    /// - selected category and difficulty
    /// - score tracking
    /// - round state management
    ///
    /// Individual game modes implement their own question selection and answer evaluation logic.
    /// </summary>
    public abstract class GameModeBase : MonoBehaviour, IGameMode
    {
        protected string currentCategory;
        protected DifficultyTier currentTier;
        protected int targetQuestionCount;

        protected int correctCount;
        protected int totalAnswered;

        /// <summary>
        /// Initializes common game mode state before a round starts.
        /// </summary>
        public virtual void Setup(string category, DifficultyTier tier, int questionCount)
        {
            currentCategory = category;
            currentTier = tier;
            targetQuestionCount = questionCount;
            correctCount = 0;
            totalAnswered = 0;
        }

        public abstract void StartRound();

        // Different game modes may use different answer types, therefore EvaluateAnswer() receives an object parameter.
        public abstract void EvaluateAnswer(object answer);

        /// <summary>
        /// Called when the round has finished.
        /// Derived classes may override this to provide additional end-of-round behaviour.
        /// </summary>
        public virtual void EndSession()
        {
            Debug.Log($"[{GetType().Name}] Session ended. {correctCount}/{totalAnswered} correct.");
        }
    }
}
