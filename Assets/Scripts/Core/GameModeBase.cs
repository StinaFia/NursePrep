using UnityEngine;

namespace NursingQuiz.Core
{
    /// <summary>
    /// Optional base class for game modes. Handles the boilerplate every mode needs
    /// (score tracking, session state) so individual modes only implement their own logic.
    /// </summary>
    public abstract class GameModeBase : MonoBehaviour, IGameMode
    {
        protected string currentAihe;
        protected DifficultyTier currentTier;
        protected int targetQuestionCount;

        protected int correctCount;
        protected int totalAnswered;

        public virtual void Setup(string aihe, DifficultyTier tier, int questionCount)
        {
            currentAihe = aihe;
            currentTier = tier;
            targetQuestionCount = questionCount;
            correctCount = 0;
            totalAnswered = 0;
        }

        public abstract void StartRound();
        public abstract void EvaluateAnswer(object answer);

        public virtual void EndSession()
        {
            Debug.Log($"[{GetType().Name}] Session ended. {correctCount}/{totalAnswered} correct.");
        }
    }
}
