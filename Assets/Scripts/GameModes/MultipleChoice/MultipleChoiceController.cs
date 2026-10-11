using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NursingQuiz.Core;
using NursingQuiz.Data;

namespace NursingQuiz.GameModes.MultipleChoice
{
    /// <summary>
    /// Drives one multiple-choice play session: loads QuestionBanks matching the chosen
    /// category + tier, samples a subset, and steps through questions one at a time.
    ///
    /// This class only holds game logic/state -- UI wiring (buttons, feedback panel) belongs
    /// in a separate view script that listens to this controller. Not built yet; this is the
    /// next step after confirming the import pipeline works.
    /// </summary>
    public class MultipleChoiceController : GameModeBase
    {
        [Tooltip("Drag the imported QuestionBank assets this mode can draw from.")]
        [SerializeField] private List<QuestionBank> availableBanks = new List<QuestionBank>();

        private List<QuestionData> roundQuestions;
        private int currentIndex;
        private readonly HashSet<int> currentSelection = new HashSet<int>();

        public QuestionData CurrentQuestion =>
            (roundQuestions != null && currentIndex < roundQuestions.Count) ? roundQuestions[currentIndex] : null;

        public int CurrentQuestionNumber => currentIndex + 1;
        public int TotalQuestionsThisRound => roundQuestions?.Count ?? 0;
        public bool HasSelection => currentSelection.Count > 0;
        public bool RoundFinished => roundQuestions != null && currentIndex >= roundQuestions.Count;

        public QuestionData LastAnsweredQuestion { get; private set; }
        public bool LastAnswerWasCorrect { get; private set; }

        public int CorrectCount => correctCount;
        public int TotalAnswered => totalAnswered;

        /// <summary>Assign banks at runtime (e.g. from a test-scene bootstrapper). The Inspector list still works too.</summary>
        public void SetAvailableBanks(IEnumerable<QuestionBank> banks)
        {
            availableBanks = banks.ToList();
        }

        public override void StartRound()
        {
            var pool = availableBanks
                .Where(b => b.category == currentAihe && b.tier == currentTier)
                .SelectMany(b => b.questions)
                .ToList();

            if (pool.Count == 0)
            {
                Debug.LogError($"No questions found for aihe={currentAihe}, tier={currentTier}. " +
                                "Check that the bank was imported and is assigned in the inspector.");
                roundQuestions = new List<QuestionData>();
                return;
            }

            roundQuestions = Shuffle(pool).Take(Mathf.Min(targetQuestionCount, pool.Count)).ToList();
            currentIndex = 0;
            currentSelection.Clear();
        }

        /// <summary>Call from UI when the player taps/clicks an answer option.</summary>
        public void ToggleOption(int optionIndex)
        {
            var q = CurrentQuestion;
            if (q == null) return;

            if (!q.AllowsMultiSelect)
            {
                currentSelection.Clear();
                currentSelection.Add(optionIndex);
                return;
            }

            if (!currentSelection.Add(optionIndex))
                currentSelection.Remove(optionIndex);
        }

        public bool IsOptionSelected(int optionIndex) => currentSelection.Contains(optionIndex);

        /// <summary>Call from UI when the player submits their current selection.</summary>
        public override void EvaluateAnswer(object answer)
        {
            var q = CurrentQuestion;
            if (q == null) return;

            bool correct = q.IsAnswerCorrect(currentSelection);
            totalAnswered++;
            if (correct) correctCount++;

            LastAnsweredQuestion = q;
            LastAnswerWasCorrect = correct;

            currentIndex++;
            currentSelection.Clear();

            if (currentIndex >= roundQuestions.Count)
                EndSession();
        }

        private static List<T> Shuffle<T>(List<T> list)
        {
            var copy = new List<T>(list);
            var rng = new System.Random();
            for (int i = copy.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (copy[i], copy[j]) = (copy[j], copy[i]);
            }
            return copy;
        }
    }
}
