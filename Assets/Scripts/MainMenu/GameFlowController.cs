using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NursingQuiz.Core;
using NursingQuiz.Data;
using NursingQuiz.GameModes.MultipleChoice;
using NursingQuiz.GameModes.MatchPairs;

namespace NursingQuiz.UI
{
    /// <summary>
    /// Owns screen navigation: Main Menu -> combined Theme + Difficulty Select -> a game screen
    /// (multiple choice or match pairs).
    ///
    /// IMPORTANT: a "theme" is identified by aihe+aihekori TOGETHER, not aihe alone -- one aihe
    /// (e.g. "LAA") can have multiple distinct themes under it with different aihekori (e.g.
    /// "PER" and "TUR"). All theme-identity comparisons below use both fields; using aihe alone
    /// would incorrectly treat different themes under the same aihe as the same theme.
    ///
    /// No theme is selected by default when this screen opens -- both the theme list and the
    /// difficulty side start empty/neutral until the player actively picks a theme.
    /// </summary>
    public class GameFlowController : MonoBehaviour
    {
        [Header("Content (drag every imported asset here)")]
        [SerializeField] private List<ThemeInfo> availableThemes = new List<ThemeInfo>();
        [SerializeField] private List<QuestionBank> availableBanks = new List<QuestionBank>();
        [SerializeField] private List<MatchPairSet> availableMatchPairSets = new List<MatchPairSet>();
        [SerializeField] private int questionsPerRound = 5;

        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject themeAndDifficultyPanel;
        [SerializeField] private GameObject quizScreen;
        [SerializeField] private GameObject matchPairScreen;

        [Header("Main Menu")]
        [SerializeField] private Button multipleChoiceModeButton;
        [SerializeField] private Button matchPairsModeButton;

        [Header("Theme + Difficulty Select - Left column (themes)")]
        [SerializeField] private Transform themeButtonContainer;
        [SerializeField] private ThemeButtonUI themeButtonTemplate;

        [Header("Theme + Difficulty Select - Right column (difficulty)")]
        [SerializeField] private TMP_Text themeObjectiveText;
        [SerializeField] private Button t1Button;
        [SerializeField] private Image t1ButtonBackground;
        [SerializeField] private TMP_Text t1StatusText;
        [SerializeField] private Button t2Button;
        [SerializeField] private Image t2ButtonBackground;
        [SerializeField] private TMP_Text t2StatusText;
        [SerializeField] private Button t3Button;
        [SerializeField] private Image t3ButtonBackground;
        [SerializeField] private TMP_Text t3StatusText;

        [SerializeField] private Button startGameButton;
        [SerializeField] private Button selectBackButton;

        [Header("Highlight colors")]
        [SerializeField] private Color normalColor = new Color(0.953f, 0.957f, 0.945f); // #F3F4F1
        [SerializeField] private Color selectedColor = new Color(0.812f, 0.890f, 0.941f); // #CFE3F0

        [Header("Multiple choice")]
        [SerializeField] private MultipleChoiceView quizView;
        [SerializeField] private Button quizBackToMenuButton;

        [Header("Match pairs")]
        [SerializeField] private MatchPairView matchPairView;
        [SerializeField] private Button matchPairBackToMenuButton;

        private string selectedAihe;
        private string selectedAihekori;
        private DifficultyTier? selectedTier;
        private GameModeType selectedMode;
        private readonly List<ThemeButtonUI> spawnedThemeButtons = new List<ThemeButtonUI>();

        private void Awake()
        {
            multipleChoiceModeButton.onClick.AddListener(() => { selectedMode = GameModeType.MultipleChoice; ShowThemeAndDifficultySelect(); });
            matchPairsModeButton.onClick.AddListener(() => { selectedMode = GameModeType.MatchPairs; ShowThemeAndDifficultySelect(); });

            selectBackButton.onClick.AddListener(ShowMainMenu);
            t1Button.onClick.AddListener(() => SelectTier(DifficultyTier.T1));
            t2Button.onClick.AddListener(() => SelectTier(DifficultyTier.T2));
            t3Button.onClick.AddListener(() => SelectTier(DifficultyTier.T3));
            startGameButton.onClick.AddListener(StartGame);

            quizBackToMenuButton.onClick.AddListener(ShowMainMenu);
            matchPairBackToMenuButton.onClick.AddListener(ShowMainMenu);

            themeButtonTemplate.gameObject.SetActive(false);

            ShowMainMenu();
        }

        private void ShowMainMenu() => SetActivePanel(mainMenuPanel);

        private void ShowThemeAndDifficultySelect()
        {
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
                clone.Aihekori = theme.aihekori;

                var capturedAihe = theme.aihe;
                var capturedAihekori = theme.aihekori;
                clone.Button.onClick.AddListener(() => SelectTheme(capturedAihe, capturedAihekori));

                spawnedThemeButtons.Add(clone);
            }

            SetActivePanel(themeAndDifficultyPanel);

            ResetDifficultySide();
        }

        /// <summary>Clears the right column back to "nothing chosen yet" -- called on entry and whenever no theme is selected.</summary>
        private void ResetDifficultySide()
        {
            selectedAihe = null;
            selectedAihekori = null;
            selectedTier = null;

            startGameButton.gameObject.SetActive(false);
            themeObjectiveText.text = "";

            t1Button.interactable = false;
            t2Button.interactable = false;
            t3Button.interactable = false;
            t1StatusText.text = "";
            t2StatusText.text = "";
            t3StatusText.text = "";

            RefreshTierHighlights();

            foreach (var themeButton in spawnedThemeButtons)
                themeButton.Background.color = normalColor;
        }

        private void SelectTheme(string aihe, string aihekori)
        {
            selectedAihe = aihe;
            selectedAihekori = aihekori;
            selectedTier = null;
            startGameButton.gameObject.SetActive(false);

            var theme = availableThemes.FirstOrDefault(t => t.aihe == aihe && t.aihekori == aihekori);
            themeObjectiveText.text = theme != null ? theme.oppimistavoite : "";

            SetupTierButton(t1Button, t1StatusText, aihe, aihekori, DifficultyTier.T1);
            SetupTierButton(t2Button, t2StatusText, aihe, aihekori, DifficultyTier.T2);
            SetupTierButton(t3Button, t3StatusText, aihe, aihekori, DifficultyTier.T3);

            RefreshTierHighlights();

            foreach (var themeButton in spawnedThemeButtons)
                themeButton.Background.color = (themeButton.Aihe == aihe && themeButton.Aihekori == aihekori) ? selectedColor : normalColor;
        }

        /// <summary>
        /// Enables/disables a tier button and shows a short availability status. Filters by
        /// aihe+aihekori together -- see the class-level note on why aihe alone isn't enough.
        /// </summary>
        private void SetupTierButton(Button button, TMP_Text statusText, string aihe, string aihekori, DifficultyTier tier)
        {
            bool available = selectedMode == GameModeType.MultipleChoice
                ? availableBanks.Any(b => b.aihe == aihe && b.aihekori == aihekori && b.tier == tier && b.questions.Count > 0)
                : availableMatchPairSets.Any(s => s.aihe == aihe && s.aihekori == aihekori && s.tier == tier && s.pairs.Count > 0);

            button.interactable = available;
            statusText.text = available ? "Saatavilla" : "Ei vielä saatavilla";
        }

        private void SelectTier(DifficultyTier tier)
        {
            selectedTier = tier;
            startGameButton.gameObject.SetActive(true);
            RefreshTierHighlights();
        }

        private void RefreshTierHighlights()
        {
            t1ButtonBackground.color = selectedTier == DifficultyTier.T1 ? selectedColor : normalColor;
            t2ButtonBackground.color = selectedTier == DifficultyTier.T2 ? selectedColor : normalColor;
            t3ButtonBackground.color = selectedTier == DifficultyTier.T3 ? selectedColor : normalColor;
        }

        private void StartGame()
        {
            if (selectedTier == null) return;
            var tier = selectedTier.Value;

            if (selectedMode == GameModeType.MultipleChoice)
            {
                var banksForTheme = availableBanks
                    .Where(b => b.aihe == selectedAihe && b.aihekori == selectedAihekori)
                    .ToList();
                SetActivePanel(quizScreen);
                quizView.BeginRound(banksForTheme, selectedAihe, tier, questionsPerRound);
            }
            else
            {
                var setsForTheme = availableMatchPairSets
                    .Where(s => s.aihe == selectedAihe && s.aihekori == selectedAihekori)
                    .ToList();
                SetActivePanel(matchPairScreen);
                matchPairView.BeginRound(setsForTheme, selectedAihe, tier);
            }
        }

        private void SetActivePanel(GameObject panelToShow)
        {
            mainMenuPanel.SetActive(panelToShow == mainMenuPanel);
            themeAndDifficultyPanel.SetActive(panelToShow == themeAndDifficultyPanel);
            quizScreen.SetActive(panelToShow == quizScreen);
            matchPairScreen.SetActive(panelToShow == matchPairScreen);
        }
    }
}
