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
    /// "PER" and "TUR"). All theme-identity comparisons below use both fields.
    ///
    /// No theme is selected by default when the theme+difficulty screen opens. Each theme
    /// button shows its Oppimistavoite directly (not behind a popup), so the player sees every
    /// theme's objective at a glance before choosing.
    /// </summary>
    public class GameFlowController : MonoBehaviour

    {
        [Header("Category select")]
        [SerializeField] private List<CategoryInfo> categories = new List<CategoryInfo>();
        [SerializeField] private GameObject categorySelectPanel;
        [SerializeField] private Transform categoryButtonContainer;
        [SerializeField] private ThemeButtonUI categoryButtonTemplate;
        [SerializeField] private Button categoryBackButton;

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

        [Header("Multiple choice - in-game top bar (visible while playing, not just at the end)")]
        [SerializeField] private Button quizTopBarBackToThemeButton;
        [SerializeField] private Button quizTopBarBackToMenuButton;
        [SerializeField] private TMP_Text quizTopBarThemeText;

        [Header("Match pairs")]
        [SerializeField] private MatchPairView matchPairView;
        [SerializeField] private Button matchPairBackToMenuButton;

        private string selectedAihe;
        private string selectedAihekori;
        private string selectedCategory;
        private DifficultyTier? selectedTier;
        private GameModeType selectedMode;
        private readonly List<ThemeButtonUI> spawnedThemeButtons = new List<ThemeButtonUI>();

        private void Awake()
        {
            //multipleChoiceModeButton.onClick.AddListener(() => { selectedMode = GameModeType.MultipleChoice; ShowThemeAndDifficultySelect(); });
            multipleChoiceModeButton.onClick.AddListener(() => { selectedMode = GameModeType.MultipleChoice; ShowCategorySelect(); });
            //matchPairsModeButton.onClick.AddListener(() => { selectedMode = GameModeType.MatchPairs; ShowThemeAndDifficultySelect(); });

            categoryBackButton.onClick.AddListener(ShowMainMenu);

            //selectBackButton.onClick.AddListener(ShowMainMenu);
            selectBackButton.onClick.AddListener(ShowCategorySelect);

            categoryButtonTemplate.gameObject.SetActive(false);

            t1Button.onClick.AddListener(() => SelectTier(DifficultyTier.T1));
            t2Button.onClick.AddListener(() => SelectTier(DifficultyTier.T2));
            t3Button.onClick.AddListener(() => SelectTier(DifficultyTier.T3));
            startGameButton.onClick.AddListener(StartGame);

            quizBackToMenuButton.onClick.AddListener(ShowMainMenu);
            quizTopBarBackToThemeButton.onClick.AddListener(ShowThemeAndDifficultySelect);
            quizTopBarBackToMenuButton.onClick.AddListener(ShowMainMenu);
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

            foreach (var theme in availableThemes.Where(t => t.aihe == selectedCategory))
            {
                var clone = Instantiate(themeButtonTemplate, themeButtonContainer);
                clone.gameObject.SetActive(true);
                clone.Label.text = string.IsNullOrEmpty(theme.displayName) ? theme.aihekori : theme.displayName;
                clone.ObjectiveText.text = theme.oppimistavoite;
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

        private void ShowCategorySelect()
        {
            foreach (Transform child in categoryButtonContainer)
            {
                if (child == categoryButtonTemplate.transform) continue;
                Destroy(child.gameObject);
            }

            foreach (var aihe in availableThemes.Select(t => t.aihe).Distinct())
            {
                var info = categories.FirstOrDefault(c => c.aihe == aihe);
                var clone = Instantiate(categoryButtonTemplate, categoryButtonContainer);
                clone.gameObject.SetActive(true);
                clone.Label.text = info != null && !string.IsNullOrEmpty(info.displayName) ? info.displayName : aihe;
                clone.ObjectiveText.text = info != null ? info.description : "";

                var captured = aihe;
                clone.Button.onClick.AddListener(() =>
                {
                    selectedCategory = captured;
                    ShowThemeAndDifficultySelect();
                });
            }

            SetActivePanel(categorySelectPanel);
        }

        private void SelectTheme(string aihe, string aihekori)
        {
            selectedAihe = aihe;
            selectedAihekori = aihekori;
            selectedTier = null;
            startGameButton.gameObject.SetActive(false);

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

                var theme = availableThemes.FirstOrDefault(t => t.aihe == selectedAihe && t.aihekori == selectedAihekori);
                quizTopBarThemeText.text = theme != null
                    ? (string.IsNullOrEmpty(theme.displayName) ? theme.aihekori : theme.displayName)
                    : "";

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
            categorySelectPanel.SetActive(panelToShow == categorySelectPanel);
            themeAndDifficultyPanel.SetActive(panelToShow == themeAndDifficultyPanel);
            quizScreen.SetActive(panelToShow == quizScreen);
            matchPairScreen.SetActive(panelToShow == matchPairScreen);
        }
    }
}
