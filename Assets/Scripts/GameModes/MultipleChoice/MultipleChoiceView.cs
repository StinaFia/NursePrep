using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NursingQuiz.Core;
using NursingQuiz.Data;

namespace NursingQuiz.GameModes.MultipleChoice
{
    /// <summary>
    /// Binds a hand-built Unity UI to MultipleChoiceController. Build the actual screen in the
    /// Editor (Canvas, buttons, text, feedback panel) and drag each piece onto the matching
    /// field below -- this script has no UI-construction code, only wiring and game-flow logic.
    ///
    /// See the README for the recommended hierarchy to build before wiring this up.
    /// </summary>
    [RequireComponent(typeof(MultipleChoiceController))]
    public class MultipleChoiceView : MonoBehaviour
    {
        [Header("Round setup")]
        [SerializeField] private int defaultQuestionCount = 5;

        [Header("Question")]
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private TMP_Text progressText;

        [Header("Answer options (assign in order: A, B, C, D)")]
        [SerializeField] private List<AnswerOptionUI> options = new List<AnswerOptionUI>();

        [Header("Submit")]
        [SerializeField] private Button submitButton;

        [Header("Feedback panel")]
        [SerializeField] private GameObject feedbackPanel;
        [SerializeField] private TMP_Text correctAnswerText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private TMP_Text professionalText;
        [SerializeField] private Button nextButton;

        [Header("Round complete panel")]
        [SerializeField] private GameObject completePanel;
        [SerializeField] private TMP_Text completeText;

        [Header("Colors")]
        [SerializeField] private Color normalOptionColor = new Color(0.953f, 0.957f, 0.945f); // #F3F4F1
        [SerializeField] private Color selectedOptionColor = new Color(0.812f, 0.890f, 0.941f); // #CFE3F0
        [SerializeField] private Color correctColor = new Color(0.812f, 0.910f, 0.827f); // #CFE8D3

        private MultipleChoiceController controller;

        private void Awake()
        {
            controller = GetComponent<MultipleChoiceController>();

            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            for (int i = 0; i < options.Count; i++)
            {
                int captured = i;
                options[i].Button.onClick.AddListener(() => OnOptionClicked(captured));
            }
            submitButton.onClick.AddListener(OnSubmitClicked);
            nextButton.onClick.AddListener(OnNextClicked);

            feedbackPanel.SetActive(false);
            completePanel.SetActive(false);
        }

        /// <summary>
        /// Starts a round for the given theme/tier, using whichever bank in the supplied list
        /// matches. Called by GameFlowController once the player has picked both a theme and
        /// a difficulty -- this is the real entry point now, not Awake/Start, since the same
        /// view needs to be reusable across multiple rounds without the player restarting the game.
        /// </summary>
        public void BeginRound(List<QuestionBank> banks, string aihe, DifficultyTier tier, int questionCount = -1)
        {
            ResetUIForNewRound();

            controller.SetAvailableBanks(banks);

            if (!banks.Any(b => b.aihe == aihe && b.tier == tier))
            {
                var available = string.Join(", ", banks.Select(b => $"{b.aihe}/{b.tier}"));
                Debug.LogError($"MultipleChoiceView: no bank matches aihe='{aihe}', tier={tier}. " +
                                $"Available: [{available}].");
                return;
            }

            controller.Setup(aihe, tier, questionCount > 0 ? questionCount : defaultQuestionCount);
            controller.StartRound();

            RenderCurrentQuestion();
        }

        /// <summary>
        /// Re-activates everything ShowRoundComplete() hid last time, so a second (or third...)
        /// round works correctly instead of leaving the question UI permanently hidden after
        /// the first round ends. Without this, going Menu -> Play again would show a blank screen.
        /// </summary>
        private void ResetUIForNewRound()
        {
            questionText.gameObject.SetActive(true);
            progressText.gameObject.SetActive(true);
            submitButton.gameObject.SetActive(true);
            foreach (var o in options) o.gameObject.SetActive(true);
            feedbackPanel.SetActive(false);
            completePanel.SetActive(false);
        }

        /// <summary>
        /// Checks every field is wired before touching it -- a missing Inspector reference
        /// otherwise surfaces as a NullReferenceException with no indication of which field
        /// was forgotten, which is a frustrating way to debug a hand-built hierarchy.
        /// </summary>
        private bool ValidateReferences()
        {
            var missing = new List<string>();
            if (questionText == null) missing.Add("Question Text");
            if (progressText == null) missing.Add("Progress Text");
            if (options == null || options.Count == 0) missing.Add("Options (list is empty)");
            else
            {
                for (int i = 0; i < options.Count; i++)
                {
                    if (options[i] == null) missing.Add($"Options[{i}]");
                    else
                    {
                        if (options[i].Button == null) missing.Add($"Options[{i}].Button");
                        if (options[i].Background == null) missing.Add($"Options[{i}].Background");
                        if (options[i].Label == null) missing.Add($"Options[{i}].Label");
                    }
                }
            }
            if (submitButton == null) missing.Add("Submit Button");
            if (feedbackPanel == null) missing.Add("Feedback Panel");
            if (correctAnswerText == null) missing.Add("Correct Answer Text");
            if (feedbackText == null) missing.Add("Feedback Text");
            if (professionalText == null) missing.Add("Professional Text");
            if (nextButton == null) missing.Add("Next Button");
            if (completePanel == null) missing.Add("Complete Panel");
            if (completeText == null) missing.Add("Complete Text");

            if (missing.Count > 0)
            {
                Debug.LogError($"MultipleChoiceView: missing Inspector reference(s): {string.Join(", ", missing)}. " +
                                "Drag the matching object from your hand-built hierarchy onto each field.");
                return false;
            }
            return true;
        }

        private void OnOptionClicked(int index)
        {
            controller.ToggleOption(index);
            RefreshOptionVisuals();
        }

        private void OnSubmitClicked()
        {
            if (!controller.HasSelection) return;

            controller.EvaluateAnswer(null);

            var q = controller.LastAnsweredQuestion;
            bool correct = controller.LastAnswerWasCorrect;

            for (int i = 0; i < options.Count; i++)
            {
                if (q.correctIndices.Contains(i))
                    options[i].Background.color = correctColor;
            }

            correctAnswerText.text = correct ? q.correctFeedback : q.incorrectFeedback;
            feedbackText.text = q.rationale ?? "";
            professionalText.text = string.IsNullOrEmpty(q.professionalPatch) ? "" : q.professionalPatch;

            submitButton.gameObject.SetActive(false);
            feedbackPanel.SetActive(true);
        }

        private void OnNextClicked()
        {
            feedbackPanel.SetActive(false);
            submitButton.gameObject.SetActive(true);

            if (controller.RoundFinished)
                ShowRoundComplete();
            else
                RenderCurrentQuestion();
        }

        private void RenderCurrentQuestion()
        {
            var q = controller.CurrentQuestion;
            if (q == null)
            {
                ShowRoundComplete();
                return;
            }

            progressText.text = $"Kysymys {controller.CurrentQuestionNumber}/{controller.TotalQuestionsThisRound}";
            questionText.text = q.questionText;

            for (int i = 0; i < options.Count; i++)
            {
                bool hasOption = i < q.options.Count;
                options[i].gameObject.SetActive(hasOption);
                if (hasOption) options[i].Label.text = q.options[i];
            }

            RefreshOptionVisuals();
            submitButton.interactable = false;
        }

        private void RefreshOptionVisuals()
        {
            for (int i = 0; i < options.Count; i++)
                options[i].Background.color = controller.IsOptionSelected(i) ? selectedOptionColor : normalOptionColor;

            submitButton.interactable = controller.HasSelection;
        }

        private void ShowRoundComplete()
        {
            questionText.gameObject.SetActive(false);
            progressText.gameObject.SetActive(false);
            foreach (var o in options) o.gameObject.SetActive(false);
            submitButton.gameObject.SetActive(false);
            feedbackPanel.SetActive(false);

            completeText.text = $"Valmis!\n\nOikein: {controller.CorrectCount} / {controller.TotalAnswered}";
            completePanel.SetActive(true);
        }
    }
}
