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
    /// Owns screen navigation: Main Menu -> combined Theme + Difficulty Select -> Quiz.
    /// Holds all imported content (themes + question banks) and hands the relevant slice
    /// to MultipleChoiceView once the player has made both choices.
    ///
    /// Theme and difficulty are on ONE screen (theme list left, difficulty right) --
    /// clicking a theme updates the difficulty side in place rather than navigating to a
    /// new screen. The first available theme is auto-selected on entry so the difficulty
    /// side is never empty.
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
        [SerializeField] private GameObject themeAndDifficultyPanel;
        [SerializeField] private GameObject quizScreen;

        [Header("Main Menu")]
        [SerializeField] private Button multipleChoiceModeButton;

        [Header("Theme + Difficulty Select - Left column (themes)")]
        [SerializeField] private Transform themeButtonContainer;
        [SerializeField] private ThemeButtonUI themeButtonTemplate;

        [Header("Theme + Difficulty Select - Right column (difficulty)")]
        [SerializeField] private Button t1Button;
        [SerializeField] private TMP_Text t1ObjectiveText;
        [SerializeField] private Button t2Button;
        [SerializeField] private TMP_Text t2ObjectiveText;
        [SerializeField] private Button t3Button;
        [SerializeField] private TMP_Text t3ObjectiveText;

        [SerializeField] private Button selectBackButton;

        [Header("Theme highlight colors")]
        [SerializeField] private Color themeNormalColor = new Color(0.953f, 0.957f, 0.945f); // #F3F4F1
        [SerializeField] private Color themeSelectedColor = new Color(0.812f, 0.890f, 0.941f); // #CFE3F0

        [Header("Quiz")]
        [SerializeField] private MultipleChoiceView quizView;

        [Header("Results (the quiz screen's own complete panel needs one extra button)")]
        [SerializeField] private Button backToMenuButton;

        private string selectedAihe;
        private readonly List<ThemeButtonUI> spawnedThemeButtons = new List<ThemeButtonUI>();

        private void Awake()
        {
            multipleChoiceModeButton.onClick.AddListener(ShowThemeAndDifficultySelect);
            selectBackButton.onClick.AddListener(ShowMainMenu);
            t1Button.onClick.AddListener(() => StartQuiz(DifficultyTier.T1));
            t2Button.onClick.AddListener(() => StartQuiz(DifficultyTier.T2));
            t3Button.onClick.AddListener(() => StartQuiz(DifficultyTier.T3));
            backToMenuButton.onClick.AddListener(ShowMainMenu);

            themeButtonTemplate.gameObject.SetActive(false);

            ShowMainMenu();
        }

        private void ShowMainMenu() => SetActivePanel(mainMenuPanel);

        private void ShowThemeAndDifficultySelect()
        {
            // Clear any buttons from a previous visit, but keep the template itself.
            foreach (Transform child in themeButtonContainer)
            {
                if (child == themeButtonTemplate.transform) continue;
                Destroy(child.gameObject);
            }
            spawnedThemeButtons.Clear();

            foreach (var theme in availableThemes)
            {
                var clone = Instantiate(themeButtonTemplate, themeButtonContainer);
                clone.gameObject.SetActive(true);
                clone.Label.text = string.IsNullOrEmpty(theme.displayName) ? theme.aihekori : theme.displayName;
                clone.Aihe = theme.aihe;

                var capturedAihe = theme.aihe;
                clone.Button.onClick.AddListener(() => SelectTheme(capturedAihe));

                spawnedThemeButtons.Add(clone);
            }

            SetActivePanel(themeAndDifficultyPanel);

            // Auto-select the first theme so the difficulty side is never empty on entry.
            if (availableThemes.Count > 0)
                SelectTheme(availableThemes[0].aihe);
        }

        /// <summary>
        /// Updates the right-hand (difficulty) side in place and highlights the chosen theme
        /// on the left -- no panel switch, this is what makes it feel like one combined screen.
        /// </summary>
        private void SelectTheme(string aihe)
        {
            selectedAihe = aihe;

            SetupTierButton(t1Button, t1ObjectiveText, aihe, DifficultyTier.T1);
            SetupTierButton(t2Button, t2ObjectiveText, aihe, DifficultyTier.T2);
            SetupTierButton(t3Button, t3ObjectiveText, aihe, DifficultyTier.T3);

            foreach (var themeButton in spawnedThemeButtons)
                themeButton.Background.color = themeButton.Aihe == aihe ? themeSelectedColor : themeNormalColor;
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
            themeAndDifficultyPanel.SetActive(panelToShow == themeAndDifficultyPanel);
            quizScreen.SetActive(panelToShow == quizScreen);
        }
    }
}
