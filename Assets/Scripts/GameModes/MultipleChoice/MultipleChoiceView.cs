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
        [Header("Test data (temporary -- a real menu will set aihe/tier at runtime later)")]
        [Tooltip("Drag ALL your imported QuestionBank assets here, across every theme and tier. " +
                 "The Test Aihe / Test Tier fields below pick which one actually plays -- this is " +
                 "exactly what a real theme/difficulty menu will do automatically once it exists.")]
        [SerializeField] private List<QuestionBank> testBanks = new List<QuestionBank>();
        [SerializeField] private string testAihe = "LAA";
        [SerializeField] private DifficultyTier testTier = DifficultyTier.T1;
        [SerializeField] private int questionCount = 5;

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
        [SerializeField] private Color normalOptionColor = Color.white;
        [SerializeField] private Color selectedOptionColor = new Color(0.55f, 0.75f, 1f);
        [SerializeField] private Color correctColor = new Color(0.6f, 0.85f, 0.6f);

        private MultipleChoiceController controller;

        private void Start()
        {
            controller = GetComponent<MultipleChoiceController>();

            if (!ValidateReferences()) return;

            for (int i = 0; i < options.Count; i++)
            {
                int captured = i;
                options[i].Button.onClick.AddListener(() => OnOptionClicked(captured));
            }
            submitButton.onClick.AddListener(OnSubmitClicked);
            nextButton.onClick.AddListener(OnNextClicked);

            feedbackPanel.SetActive(false);
            completePanel.SetActive(false);

            controller.SetAvailableBanks(testBanks);

            if (!testBanks.Any(b => b.aihe == testAihe && b.tier == testTier))
            {
                var available = string.Join(", ", testBanks.Select(b => $"{b.aihe}/{b.tier}"));
                Debug.LogError($"MultipleChoiceView: no bank in Test Banks matches aihe='{testAihe}', tier={testTier}. " +
                                $"Available combinations: [{available}]. Check for typos in Test Aihe.");
                return;
            }

            controller.Setup(testAihe, testTier, questionCount);
            controller.StartRound();

            RenderCurrentQuestion();
        }

        /// <summary>
        /// Checks every field is wired before touching it -- a missing Inspector reference
        /// otherwise surfaces as a NullReferenceException with no indication of which field
        /// was forgotten, which is a frustrating way to debug a hand-built hierarchy.
        /// </summary>
        private bool ValidateReferences()
        {
            var missing = new List<string>();
            if (testBanks == null || testBanks.Count == 0) missing.Add("Test Banks (list is empty)");
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
