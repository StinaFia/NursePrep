using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NursingQuiz.Core;
using NursingQuiz.Data;
using NursingQuiz.GameModes.MultipleChoice;

namespace NursingQuiz.UI
{
    /// <summary>
    /// Owns screen navigation: Main Menu -> Theme Select -> Difficulty Select -> Quiz.
    /// Holds all imported content (themes + question banks) and hands the relevant slice
    /// to MultipleChoiceView once the player has made both choices.
    ///
    /// Theme buttons are generated at runtime from Available Themes -- adding a new theme
    /// later is just dragging its ThemeInfo and QuestionBank assets into the lists below,
    /// no new UI to build.
    /// </summary>
    public class GameFlowController : MonoBehaviour
    {
        [Header("Content (drag every imported asset here)")]
        [SerializeField] private List<ThemeInfo> availableThemes = new List<ThemeInfo>();
        [SerializeField] private List<QuestionBank> availableBanks = new List<QuestionBank>();
        [SerializeField] private int questionsPerRound = 5;

        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject themeSelectPanel;
        [SerializeField] private GameObject difficultySelectPanel;
        [SerializeField] private GameObject quizScreen;

        [Header("Main Menu")]
        [SerializeField] private Button playButton;

        [Header("Theme Select")]
        [SerializeField] private Transform themeButtonContainer;
        [SerializeField] private ThemeButtonUI themeButtonTemplate;
        [SerializeField] private Button themeBackButton;

        [Header("Difficulty Select")]
        [SerializeField] private TMP_Text difficultyThemeTitle;
        [SerializeField] private Button t1Button;
        [SerializeField] private TMP_Text t1ObjectiveText;
        [SerializeField] private Button t2Button;
        [SerializeField] private TMP_Text t2ObjectiveText;
        [SerializeField] private Button t3Button;
        [SerializeField] private TMP_Text t3ObjectiveText;
        [SerializeField] private Button difficultyBackButton;

        [Header("Quiz")]
        [SerializeField] private MultipleChoiceView quizView;

        [Header("Results (the quiz screen's own complete panel needs one extra button)")]
        [SerializeField] private Button backToMenuButton;

        private string selectedAihe;

        private void Awake()
        {
            playButton.onClick.AddListener(ShowThemeSelect);
            themeBackButton.onClick.AddListener(ShowMainMenu);
            difficultyBackButton.onClick.AddListener(ShowThemeSelect);
            t1Button.onClick.AddListener(() => StartQuiz(DifficultyTier.T1));
            t2Button.onClick.AddListener(() => StartQuiz(DifficultyTier.T2));
            t3Button.onClick.AddListener(() => StartQuiz(DifficultyTier.T3));
            backToMenuButton.onClick.AddListener(ShowMainMenu);

            themeButtonTemplate.gameObject.SetActive(false);

            ShowMainMenu();
        }

        private void ShowMainMenu() => SetActivePanel(mainMenuPanel);

        private void ShowThemeSelect()
        {
            // Clear any buttons from a previous visit, but keep the template itself.
            foreach (Transform child in themeButtonContainer)
            {
                if (child == themeButtonTemplate.transform) continue;
                Destroy(child.gameObject);
            }

            foreach (var theme in availableThemes)
            {
                var clone = Instantiate(themeButtonTemplate, themeButtonContainer);
                clone.gameObject.SetActive(true);
                clone.Label.text = string.IsNullOrEmpty(theme.displayName) ? theme.aihekori : theme.displayName;

                var capturedAihe = theme.aihe;
                clone.Button.onClick.AddListener(() => ShowDifficultySelect(capturedAihe));
            }

            SetActivePanel(themeSelectPanel);
        }

        private void ShowDifficultySelect(string aihe)
        {
            selectedAihe = aihe;
            difficultyThemeTitle.text = aihe;

            SetupTierButton(t1Button, t1ObjectiveText, aihe, DifficultyTier.T1);
            SetupTierButton(t2Button, t2ObjectiveText, aihe, DifficultyTier.T2);
            SetupTierButton(t3Button, t3ObjectiveText, aihe, DifficultyTier.T3);

            SetActivePanel(difficultySelectPanel);
        }

        /// <summary>
        /// Enables/disables a tier button depending on whether that tier's bank has been
        /// imported yet, and shows its Oppimistavoite -- this is the "objective visible when
        /// picking difficulty" behavior, driven directly from QuestionBank.oppimistavoite.
        /// </summary>
        private void SetupTierButton(Button button, TMP_Text objectiveText, string aihe, DifficultyTier tier)
        {
            var bank = availableBanks.FirstOrDefault(b => b.aihe == aihe && b.tier == tier);
            bool available = bank != null && bank.questions.Count > 0;

            button.interactable = available;
            objectiveText.text = available ? bank.oppimistavoite : "Ei vielä saatavilla";
        }

        private void StartQuiz(DifficultyTier tier)
        {
            var banksForTheme = availableBanks.Where(b => b.aihe == selectedAihe).ToList();
            SetActivePanel(quizScreen);
            quizView.BeginRound(banksForTheme, selectedAihe, tier, questionsPerRound);
        }

        private void SetActivePanel(GameObject panelToShow)
        {
            mainMenuPanel.SetActive(panelToShow == mainMenuPanel);
            themeSelectPanel.SetActive(panelToShow == themeSelectPanel);
            difficultySelectPanel.SetActive(panelToShow == difficultySelectPanel);
            quizScreen.SetActive(panelToShow == quizScreen);
        }
    }
}
