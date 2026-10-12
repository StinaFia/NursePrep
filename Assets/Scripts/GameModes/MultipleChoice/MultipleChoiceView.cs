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
    /// Connects the MultipleChoiceController to the Unity UI
    /// 
    /// This class is responsible for:
    /// - rendering the current question and answer options
    /// - handling player input (clicking an answer, submitting, moving to next question)
    /// - displating feedback and round results
    /// 
    /// This screen currently supports single-choice questions (T1)
    /// ans is designed to be extended to support multiple-choice questions (T2&T3) in the future.
    /// </summary>

    [RequireComponent(typeof(MultipleChoiceController))]
    public class MultipleChoiceView : MonoBehaviour
    {
        [Header("Round setup")]
        [SerializeField] private int defaultQuestionCount = 5;

        [Header("Question")]
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private ResultView resultCard;

        [Header("Answer options (assign in order: A, B, C, D)")]
        [SerializeField] private List<AnswerOptionUI> options = new List<AnswerOptionUI>();

        //[Header("Submit")]
        //[SerializeField] private Button submitButton;

        [Header("Feedback panel")]
        [SerializeField] private GameObject feedbackPanel;
        [SerializeField] private Image resultIcon;
        [SerializeField] private Sprite checkSprite;
        [SerializeField] private Sprite crossSprite;
        [SerializeField] private TMP_Text correctAnswerText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private GameObject professionalTipRow;
        [SerializeField] private TMP_Text professionalTipText;
        [SerializeField] private Button nextButton;

        [Header("Round complete panel")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text topBarThemeText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private Button retryButton;

        [Header("Colors")]
        [SerializeField] private Color normalOptionColor = new Color(0.953f, 0.957f, 0.945f); // #F3F4F1
        [SerializeField] private Color selectedOptionColor = new Color(0.812f, 0.890f, 0.941f); // #CFE3F0
        [SerializeField] private Color correctColor = new Color(0.812f, 0.910f, 0.827f); // #CFE8D3

        private MultipleChoiceController controller;

        private List<QuestionBank> lastBanks;
        private string lastAihe;
        private DifficultyTier lastTier;
        private int lastQuestionCount;
        private bool answerLocked;

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

            // Reserved for upcoming T2/T3 support where answers are evaluated only after submitting.
            //submitButton.onClick.AddListener(OnSubmitClicked);
            nextButton.onClick.AddListener(OnNextClicked);
            retryButton.onClick.AddListener(OnRetryClicked);

            feedbackPanel.SetActive(false);
            resultPanel.SetActive(false);
        }

        /// <summary>
        /// Starts a new round using the selected theme and difficulty.
        /// Called by GameFlowController after the player has completed the theme and difficulty selection flow.
        /// </summary>
        public void BeginRound(List<QuestionBank> banks, string aihe, DifficultyTier tier, int questionCount = -1)
        {
            lastBanks = banks;
            lastAihe = aihe;
            lastTier = tier;
            lastQuestionCount = questionCount;

            ResetUIForNewRound();

            controller.SetAvailableBanks(banks);

            if (!banks.Any(b => b.category == aihe && b.tier == tier))
            {
                var available = string.Join(", ", banks.Select(b => $"{b.category}/{b.tier}"));
                Debug.LogError($"MultipleChoiceView: no bank matches aihe='{aihe}', tier={tier}. " +
                                $"Available: [{available}].");
                return;
            }

            controller.Setup(aihe, tier, questionCount > 0 ? questionCount : defaultQuestionCount);
            controller.StartRound();

            RenderCurrentQuestion();
        }

        /// <summary>
        /// Restores the quiz UI to its initial state before a new round begins.
        /// </summary>
        private void ResetUIForNewRound()
        {
            answerLocked = false;
            questionText.gameObject.SetActive(true);
            progressText.gameObject.SetActive(true);
            //submitButton.gameObject.SetActive(true);
            foreach (var o in options) o.gameObject.SetActive(true);
            feedbackPanel.SetActive(false);
            resultPanel.SetActive(false);
        }

        /// <summary>
        /// Verifies that all required Inspector references have been assigned before the gameplay starts.
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
            //if (submitButton == null) missing.Add("Submit Button");
            if (feedbackPanel == null) missing.Add("Feedback Panel");
            if (correctAnswerText == null) missing.Add("Correct Answer Text");
            if (feedbackText == null) missing.Add("Feedback Text");
            if (professionalTipRow == null) missing.Add("Professional Tip Row");
            if (professionalTipText == null) missing.Add("Professional Tip Text");
            if (nextButton == null) missing.Add("Next Button");
            if (resultPanel == null) missing.Add("Result Panel");
            if (scoreText == null) missing.Add("Score Text");

            if (missing.Count > 0)
            {
                Debug.LogError($"MultipleChoiceView: missing Inspector reference(s): {string.Join(", ", missing)}. " +
                                "Drag the matching object from your hand-built hierarchy onto each field.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Handles answer option selection.
        ///
        /// Current implementation is optimized for T1:
        /// selecting an answer immediately evaluates it.
        ///
        /// Future T2/T3 support should move the evaluation step behind an explicit Submit button.
        /// </summary>
        /// 

        // TODO:
        // For T2/T3 multiple-selection gameplay,
        // move EvaluateAnswer() into a dedicated Submit button handler.
        private void OnOptionClicked(int index)
        {
            if (answerLocked) return;

            // Select or deselect the clicked answer option.
            controller.ToggleOption(index);
            RefreshOptionVisuals();

            // Do nothing if no option is currently selected.

            if (!controller.HasSelection)
                return;

            // T1 behaviour:
            // evaluate the answer immediately after selection.
            // Future T2/T3 support should move this into a Submit button.
            controller.EvaluateAnswer(null);
            answerLocked = true;

            var q = controller.LastAnsweredQuestion;
            bool correct = controller.LastAnswerWasCorrect;

            // Highlight all correct answer options.
            for (int i = 0; i < options.Count; i++)
            {
                if (q.correctIndices.Contains(i))
                    options[i].Background.color = correctColor;
            }

            // Display feedback and rationale.
            correctAnswerText.text = correct ? q.correctFeedback : q.incorrectFeedback;
            resultIcon.sprite = correct ? checkSprite : crossSprite;
            feedbackText.text = q.rationale ?? "";

            bool hasPatch = !string.IsNullOrEmpty(q.professionalPatch);
            professionalTipRow.SetActive(hasPatch);
            professionalTipText.text = hasPatch ? q.professionalPatch : "";

            feedbackPanel.SetActive(true);

            // Submit button reserved for future T2/T3 support.
            //submitButton.gameObject.SetActive(false);

            // Show nextButton (user decides when to continue)
            nextButton.gameObject.SetActive(true);
        }


        /*private void OnSubmitClicked()
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

            bool hasPatch = !string.IsNullOrEmpty(q.professionalPatch);
            professionalPatchRow.SetActive(hasPatch);
            professionalText.text = hasPatch ? q.professionalPatch : "";

            submitButton.gameObject.SetActive(false);
            feedbackPanel.SetActive(true);
        }*/

        /// <summary>
        /// Advances to the next question or shows the round results if the round has ended.
        /// </summary>
        private void OnNextClicked()
        {
            feedbackPanel.SetActive(false);
            //submitButton.gameObject.SetActive(true);

            if (controller.RoundFinished)
                ShowRoundComplete();
            else
                RenderCurrentQuestion();
        }

        /// <summary>
        /// Starts a new round using the same parameters as the previously completed round.
        /// </summary>
        private void OnRetryClicked()
        {
            if (lastBanks == null) return;
            BeginRound(lastBanks, lastAihe, lastTier, lastQuestionCount);
        }

        /// <summary>
        /// Updates all question UI elements to display the current question provided by the controller.
        /// </summary>
        private void RenderCurrentQuestion()
        {
            answerLocked = false;

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
            //submitButton.interactable = false;
        }

        /// <summary>
        /// Updates answer option highlighting based on the controller's current selection state.
        /// </summary>
        private void RefreshOptionVisuals()
        {
            for (int i = 0; i < options.Count; i++)
                options[i].Background.color = controller.IsOptionSelected(i) ? selectedOptionColor : normalOptionColor;

            //submitButton.interactable = controller.HasSelection;
        }

        /// <summary>
        /// Hides quiz content and displays the round completion screen.
        /// </summary>
        private void ShowRoundComplete()
        {
            questionText.gameObject.SetActive(false);
            progressText.gameObject.SetActive(false);
            foreach (var o in options) o.gameObject.SetActive(false);
            //submitButton.gameObject.SetActive(false);
            feedbackPanel.SetActive(false);

            //completeText.text = $"Valmis!\n\nOikein: {controller.CorrectCount} / {controller.TotalAnswered}";
            //completePanel.SetActive(true);
            resultCard.Show(controller.CorrectCount, controller.TotalAnswered, topBarThemeText.text);
            resultPanel.SetActive(true);
        }
    }
}
