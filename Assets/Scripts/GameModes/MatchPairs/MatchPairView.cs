using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NursingQuiz.Core;
using NursingQuiz.Data;

namespace NursingQuiz.GameModes.MatchPairs
{
    /// <summary>
    /// Binds a hand-built Unity UI to MatchPairController. Left and right item buttons are
    /// spawned at runtime from a template (count varies per content set, unlike the fixed
    /// four options in multiple choice) -- build ONE left template and ONE right template in
    /// the Editor, wire everything else below, and this handles the rest.
    /// </summary>
    [RequireComponent(typeof(MatchPairController))]
    public class MatchPairView : MonoBehaviour
    {
        [Header("Round setup")]
        [SerializeField] private int defaultQuestionCount = -1; // -1 = use every pair in the set

        [Header("Header")]
        [SerializeField] private TMP_Text gameNameText;
        [SerializeField] private TMP_Text progressText;

        [Header("Left / Right columns")]
        [SerializeField] private Transform leftColumnContainer;
        [SerializeField] private MatchItemUI leftItemTemplate;
        [SerializeField] private Transform rightColumnContainer;
        [SerializeField] private MatchItemUI rightItemTemplate;

        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(0.953f, 0.957f, 0.945f); // #F3F4F1
        [SerializeField] private Color selectedColor = new Color(0.812f, 0.890f, 0.941f); // #CFE3F0
        [SerializeField] private Color incorrectFlashColor = new Color(0.9f, 0.6f, 0.6f);
        [Tooltip("Cycled through for matched pairs so different pairs get visibly different colors.")]
        [SerializeField]
        private Color[] matchedPalette =
        {
            new Color(0.812f, 0.910f, 0.827f), // soft green
            new Color(0.85f, 0.80f, 0.95f),    // soft purple
            new Color(0.95f, 0.85f, 0.70f),    // soft amber
            new Color(0.80f, 0.90f, 0.95f),    // soft sky blue
            new Color(0.95f, 0.80f, 0.85f),    // soft rose
            new Color(0.85f, 0.95f, 0.80f),    // soft lime
        };

        [Header("Completion panel")]
        [SerializeField] private GameObject completePanel;
        [SerializeField] private TMP_Text completionFeedbackText;
        [SerializeField] private GameObject professionalPatchRow;
        [SerializeField] private TMP_Text professionalText;

        private MatchPairController controller;
        private readonly List<MatchItemUI> leftItems = new List<MatchItemUI>();
        private readonly List<MatchItemUI> rightItems = new List<MatchItemUI>();

        private void Awake()
        {
            controller = GetComponent<MatchPairController>();

            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            leftItemTemplate.gameObject.SetActive(false);
            rightItemTemplate.gameObject.SetActive(false);
            completePanel.SetActive(false);
        }

        public void BeginRound(List<MatchPairSet> sets, string aihe, DifficultyTier tier)
        {
            controller.SetAvailableSets(sets);

            if (!sets.Any(s => s.aihe == aihe && s.tier == tier))
            {
                var available = string.Join(", ", sets.Select(s => $"{s.aihe}/{s.tier}"));
                Debug.LogError($"MatchPairView: no set matches aihe='{aihe}', tier={tier}. Available: [{available}].");
                return;
            }

            controller.Setup(aihe, tier, defaultQuestionCount);
            controller.StartRound();

            completePanel.SetActive(false);
            BuildColumns();
        }

        private bool ValidateReferences()
        {
            var missing = new List<string>();
            if (gameNameText == null) missing.Add("Game Name Text");
            if (progressText == null) missing.Add("Progress Text");
            if (leftColumnContainer == null) missing.Add("Left Column Container");
            if (leftItemTemplate == null) missing.Add("Left Item Template");
            if (rightColumnContainer == null) missing.Add("Right Column Container");
            if (rightItemTemplate == null) missing.Add("Right Item Template");
            if (completePanel == null) missing.Add("Complete Panel");
            if (completionFeedbackText == null) missing.Add("Completion Feedback Text");
            if (professionalPatchRow == null) missing.Add("Professional Patch Row");
            if (professionalText == null) missing.Add("Professional Text");

            if (missing.Count > 0)
            {
                Debug.LogError($"MatchPairView: missing Inspector reference(s): {string.Join(", ", missing)}. " +
                                "Drag the matching object from your hand-built hierarchy onto each field.");
                return false;
            }
            return true;
        }

        private void BuildColumns()
        {
            foreach (var item in leftItems) Destroy(item.gameObject);
            foreach (var item in rightItems) Destroy(item.gameObject);
            leftItems.Clear();
            rightItems.Clear();

            var set = controller.CurrentSet;
            gameNameText.text = set != null ? set.gameName : "";

            for (int slot = 0; slot < controller.PairCount; slot++)
            {
                int capturedSlot = slot;

                var leftClone = Instantiate(leftItemTemplate, leftColumnContainer);
                leftClone.gameObject.SetActive(true);
                leftClone.Label.text = controller.GetLeftLabel(slot);
                leftClone.Button.onClick.AddListener(() => OnLeftClicked(capturedSlot));
                leftItems.Add(leftClone);

                var rightClone = Instantiate(rightItemTemplate, rightColumnContainer);
                rightClone.gameObject.SetActive(true);
                rightClone.Label.text = controller.GetRightLabel(slot);
                rightClone.Button.onClick.AddListener(() => OnRightClicked(capturedSlot));
                rightItems.Add(rightClone);
            }

            RefreshVisuals();
        }

        private void OnLeftClicked(int slot)
        {
            controller.SelectLeft(slot);
            RefreshVisuals();
            HandlePendingAttempt();
        }

        private void OnRightClicked(int slot)
        {
            controller.SelectRight(slot);
            RefreshVisuals();
            HandlePendingAttempt();
        }

        private void HandlePendingAttempt()
        {
            if (!controller.HasPendingAttempt) return;

            if (!controller.LastAttemptWasCorrect)
            {
                // Brief flash handled by RefreshVisuals already having drawn the failed pair --
                // simplest approach for now is just to clear immediately; a coroutine-based
                // delay could be added later for a more visible "wrong" flash.
            }

            controller.ClearPendingAttempt();

            if (controller.RoundFinished)
                ShowCompletion();
        }

        private void RefreshVisuals()
        {
            progressText.text = $"{MatchedCount()}/{controller.PairCount}";

            for (int slot = 0; slot < leftItems.Count; slot++)
                leftItems[slot].Background.color = ColorFor(slot, isLeft: true);

            for (int slot = 0; slot < rightItems.Count; slot++)
                rightItems[slot].Background.color = ColorFor(slot, isLeft: false);
        }

        private Color ColorFor(int slot, bool isLeft)
        {
            bool matched = isLeft ? controller.IsLeftMatched(slot) : controller.IsRightMatched(slot);
            if (matched)
            {
                int colorIndex = isLeft ? controller.GetLeftColorIndex(slot) : controller.GetRightColorIndex(slot);
                return matchedPalette[colorIndex % matchedPalette.Length];
            }

            bool selected = isLeft ? controller.IsLeftSelected(slot) : controller.IsRightSelected(slot);
            return selected ? selectedColor : normalColor;
        }

        private int MatchedCount()
        {
            int count = 0;
            for (int slot = 0; slot < controller.PairCount; slot++)
                if (controller.IsLeftMatched(slot)) count++;
            return count;
        }

        private void ShowCompletion()
        {
            var set = controller.CurrentSet;

            completionFeedbackText.text = set.completionFeedback ?? "";

            bool hasPatch = !string.IsNullOrEmpty(set.professionalPatch);
            professionalPatchRow.SetActive(hasPatch);
            professionalText.text = hasPatch ? set.professionalPatch : "";

            completePanel.SetActive(true);
        }
    }
}
