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
    /// Connects the MultipleChoiceController to the Unity UI.
    ///
    /// This class is responsible for:
    /// - rendering the current question and answer options
    /// - handling player input (selecting answers, submitting, moving to the next question)
    /// - displaying feedback and round results
    ///
    /// T1: clicking an option evaluates the answer immediately.
    /// T2/T3: answer options can be toggled and the player confirms the selection using the main button ("Vastaa").
    /// After evaluation, the same button becomes "Seuraava" or "Katso tulos".
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

        [Header("Info bubble (optional)")]
        [SerializeField] private GameObject infoBubble;
        [SerializeField] private TMP_Text infoBubbleText;

        [Header("Answer options (assign in order: A, B, C, D)")]
        [SerializeField] private List<AnswerOptionUI> options = new List<AnswerOptionUI>();

        [Header("Feedback panel")]
        [SerializeField] private GameObject feedbackPanel;
        [SerializeField] private Image resultIcon;
        [SerializeField] private Sprite checkSprite;
        [SerializeField] private Sprite crossSprite;
        [SerializeField] private TMP_Text correctAnswerText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private GameObject professionalTipRow;
        [SerializeField] private TMP_Text professionalTipText;

        [Header("Main button (Vastaa / Seuraava) - keep it OUTSIDE the feedback panel")]
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text nextButtonLabel;

        [Header("Round complete panel")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text topBarThemeText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private Button retryButton;

        [Header("Colors")]
        [SerializeField] private Color normalOptionColor = new Color(0.953f, 0.957f, 0.945f); // #F3F4F1
        [SerializeField] private Color selectedOptionColor = new Color(0.812f, 0.890f, 0.941f); // #CFE3F0
        [SerializeField] private Color wrongColor = new Color(1f, 0.87f, 0.84f); // #FFDED6
        [SerializeField] private Color correctColor = new Color(0.624f, 0.827f, 0.659f);       // #9FD3A8 correct, selected
        [SerializeField] private Color missedCorrectColor = new Color(0.890f, 0.949f, 0.898f); // #E3F2E5 correct, not selected

        private MultipleChoiceController controller;

        private List<QuestionBank> lastBanks;
        private string lastCategory;
        private DifficultyTier lastTier;
        private int lastQuestionCount;

        private bool answerLocked;
        // True while the main button acts as "Vastaa" (T2/T3 before the answer is evaluated).
        private bool awaitingSubmit;

        /// <summary>
        /// Returns true for multi-select difficulty tiers (T2/T3).
        /// </summary>
        private bool IsMultiTier => lastTier != DifficultyTier.T1;

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

            nextButton.onClick.AddListener(OnMainButtonClicked);
            retryButton.onClick.AddListener(OnRetryClicked);

            feedbackPanel.SetActive(false);
            resultPanel.SetActive(false);
            nextButton.gameObject.SetActive(false);
        }

        /// <summary>
        /// Starts a new round using the selected category and difficulty.
        /// Called by GameFlowController after the player has completed the theme and difficulty selection flow.
        /// </summary>
        public void BeginRound(List<QuestionBank> banks, string category, DifficultyTier tier, int questionCount = -1)
        {
            lastBanks = banks;
            lastCategory = category;
            lastTier = tier;
            lastQuestionCount = questionCount;

            ResetUIForNewRound();

            controller.SetAvailableBanks(banks);

            if (!banks.Any(b => b.category == category && b.tier == tier))
            {
                var available = string.Join(", ", banks.Select(b => $"{b.category}/{b.tier}"));
                Debug.LogError($"MultipleChoiceView: no bank matches category='{category}', tier={tier}. " +
                                $"Available: [{available}].");
                return;
            }

            controller.Setup(category, tier, questionCount > 0 ? questionCount : defaultQuestionCount);
            controller.StartRound();

            RenderCurrentQuestion();
        }

        /// <summary>
        /// Restores the quiz UI to its initial state before a new round begins.
        /// </summary>
        private void ResetUIForNewRound()
        {
            SetInfoBubble(null);
            answerLocked = false;
            awaitingSubmit = false;

            questionText.gameObject.SetActive(true);
            progressText.gameObject.SetActive(true);
            foreach (var o in options)
            {
                o.gameObject.SetActive(true);
                o.Button.interactable = true;
            }

            feedbackPanel.SetActive(false);
            resultPanel.SetActive(false);
            nextButton.gameObject.SetActive(false);
        }

        private void SetInfoBubble(string text)
        {
            if (infoBubble == null || infoBubbleText == null) return;

            bool hasText = !string.IsNullOrWhiteSpace(text);
            infoBubble.SetActive(hasText);
            infoBubbleText.text = hasText ? text : "";
        }

        /// <summary>
        /// Verifies that all required Inspector references have been assigned before gameplay starts.
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
            if (feedbackPanel == null) missing.Add("Feedback Panel");
            if (resultIcon == null) missing.Add("Result Icon");
            if (correctAnswerText == null) missing.Add("Correct Answer Text");
            if (feedbackText == null) missing.Add("Feedback Text");
            if (professionalTipRow == null) missing.Add("Professional Tip Row");
            if (professionalTipText == null) missing.Add("Professional Tip Text");
            if (nextButton == null) missing.Add("Next Button");
            if (nextButtonLabel == null) missing.Add("Next Button Label");
            if (resultCard == null) missing.Add("Result Card");
            if (resultPanel == null) missing.Add("Result Panel");
            if (topBarThemeText == null) missing.Add("Top Bar Theme Text");
            if (scoreText == null) missing.Add("Score Text");
            if (retryButton == null) missing.Add("Retry Button");

            if (missing.Count > 0)
            {
                Debug.LogError($"MultipleChoiceView: missing Inspector reference(s): {string.Join(", ", missing)}. " +
                                "Drag the matching object from your hand-built hierarchy onto each field.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Handles answer option clicks.
        /// T1: evaluates immediately. 
        /// T2/T3: only toggles the selection; the main button submits.
        /// </summary>
        private void OnOptionClicked(int index)
        {
            if (answerLocked) return;

            controller.ToggleOption(index);
            RefreshOptionVisuals();

            if (IsMultiTier)
            {
                // T2 needs at least one selection; T3 may submit an empty selection ("none correct").
                nextButton.interactable = lastTier == DifficultyTier.T3 || controller.HasSelection;
                return;
            }

            if (!controller.HasSelection) return;
            SubmitAnswer();
        }

        /// <summary>
        /// The main button either submits the answer ("Vastaa") or advances ("Seuraava" / "Katso tulos").
        /// </summary>
        private void OnMainButtonClicked()
        {
            if (awaitingSubmit)
            {
                if (answerLocked) return;
                SubmitAnswer();
            }
            else
            {
                OnNextClicked();
            }
        }

        /// <summary>
        /// Evaluates the current selection, colors the options and shows the feedback panel.
        /// </summary>
        private void SubmitAnswer()
        {
            // Save the selection BEFORE EvaluateAnswer, because the controller clears it.
            var selected = new HashSet<int>();
            for (int i = 0; i < options.Count; i++)
                if (controller.IsOptionSelected(i)) selected.Add(i);

            controller.EvaluateAnswer(null);
            answerLocked = true;
            awaitingSubmit = false;

            // Hide the calculation hint "Infopallo" after answering.
            if (infoBubble != null)
                infoBubble.SetActive(false);

            var q = controller.LastAnsweredQuestion;
            bool correct = controller.LastAnswerWasCorrect;

            // Lock the options.
            foreach (var o in options) o.Button.interactable = false;

            // Colors:
            // correct selections darker green
            // missed correct answers lighter green
            // incorrect selections red
            for (int i = 0; i < options.Count; i++)
            {
                bool isCorrect = q.correctIndices.Contains(i);
                bool wasSelected = selected.Contains(i);

                if (isCorrect && wasSelected)
                    options[i].Background.color = correctColor;
                else if (isCorrect)
                    options[i].Background.color = missedCorrectColor;
                else if (wasSelected)
                    options[i].Background.color = wrongColor;
                else
                    options[i].Background.color = normalOptionColor;
            }

            // Display feedback and rationale.
            correctAnswerText.text = correct ? q.correctFeedback : q.incorrectFeedback;
            resultIcon.sprite = correct ? checkSprite : crossSprite;
            feedbackText.text = q.rationale ?? "";

            bool hasPatch = !string.IsNullOrEmpty(q.professionalPatch);
            professionalTipRow.SetActive(hasPatch);
            professionalTipText.text = hasPatch ? q.professionalPatch : "";

            feedbackPanel.SetActive(true);

            // The main button now continues the round. EvaluateAnswer has already advanced
            // the controller, so RoundFinished is true right after the last answer.
            nextButtonLabel.text = controller.RoundFinished ? "Katso tulos" : "Seuraava";
            nextButton.interactable = true;
            nextButton.gameObject.SetActive(true);
        }

        /// <summary>
        /// Advances to the next question or shows the round results if the round has ended.
        /// </summary>
        private void OnNextClicked()
        {
            feedbackPanel.SetActive(false);

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
            BeginRound(lastBanks, lastCategory, lastTier, lastQuestionCount);
        }

        /// <summary>
        /// Updates all question UI elements to display the current question provided by the controller.
        /// </summary>
        private void RenderCurrentQuestion()
        {
            answerLocked = false;

            if (infoBubble != null)
                infoBubble.SetActive(true);

            var q = controller.CurrentQuestion;
            if (q == null)
            {
                ShowRoundComplete();
                return;
            }

            progressText.text = $"Kysymys {controller.CurrentQuestionNumber}/{controller.TotalQuestionsThisRound}";
            questionText.text = q.questionText;

            SetInfoBubble(q.infoBubble);

            for (int i = 0; i < options.Count; i++)
            {
                bool hasOption = i < q.options.Count;
                options[i].gameObject.SetActive(hasOption);
                options[i].Button.interactable = true; // unlock for the new question
                if (hasOption) options[i].Label.text = q.options[i];
            }

            // T2/T3: the main button shows "Vastaa". T3 may submit without a selection.
            bool multi = IsMultiTier;
            awaitingSubmit = multi;
            nextButton.gameObject.SetActive(multi);
            nextButton.interactable = lastTier == DifficultyTier.T3;
            nextButtonLabel.text = "Vastaa";

            RefreshOptionVisuals();
        }

        /// <summary>
        /// Updates answer option highlighting based on the controller's current selection state.
        /// </summary>
        private void RefreshOptionVisuals()
        {
            for (int i = 0; i < options.Count; i++)
                options[i].Background.color = controller.IsOptionSelected(i) ? selectedOptionColor : normalOptionColor;
        }

        /// <summary>
        /// Hides question UI and displays the round results screen.
        /// </summary>
        private void ShowRoundComplete()
        {
            awaitingSubmit = false;

            questionText.gameObject.SetActive(false);
            SetInfoBubble(null);
            progressText.gameObject.SetActive(false);
            foreach (var o in options) o.gameObject.SetActive(false);
            feedbackPanel.SetActive(false);
            nextButton.gameObject.SetActive(false);
      
            resultCard.Show(controller.CorrectCount, controller.TotalAnswered, topBarThemeText.text);
            resultPanel.SetActive(true);
        }
    }
}