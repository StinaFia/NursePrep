using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NursingQuiz.Core;
using NursingQuiz.Data;

namespace NursingQuiz.GameModes.MatchPairs
{
    /// <summary>
    /// Drives one "Yhdistä oikein" round: picks the MatchPairSet matching the chosen
    /// category+tier, shuffles the left and right columns independently (so they don't line up
    /// trivially), and tracks tap-tap selection + matching state.
    ///
    /// Left/right columns are addressed by "slot" (0..PairCount-1, the on-screen position),
    /// which is different from "pair index" (the underlying pair's position in the source
    /// data) -- the whole point of shuffling is that slot != pair index in general.
    /// </summary>
    public class MatchPairController : GameModeBase
    {
        [Tooltip("Drag the imported MatchPairSet assets this mode can draw from.")]
        [SerializeField] private List<MatchPairSet> availableSets = new List<MatchPairSet>();

        private MatchPairSet currentSet;
        private List<int> leftOrder;  // leftOrder[slot] = pair index shown at that slot
        private List<int> rightOrder;
        private readonly HashSet<int> matchedPairIndices = new HashSet<int>();

        private int? selectedLeftSlot;
        private int? selectedRightSlot;

        public MatchPairSet CurrentSet => currentSet;
        public int PairCount => leftOrder?.Count ?? 0;
        public bool RoundFinished => currentSet != null && leftOrder != null && matchedPairIndices.Count == leftOrder.Count;

        public bool LastAttemptWasCorrect { get; private set; }
        public bool HasPendingAttempt { get; private set; }

        public void SetAvailableSets(IEnumerable<MatchPairSet> sets) => availableSets = sets.ToList();

        public string GetLeftLabel(int slot) => currentSet.pairs[leftOrder[slot]].left;
        public string GetRightLabel(int slot) => currentSet.pairs[rightOrder[slot]].right;

        public bool IsLeftMatched(int slot) => matchedPairIndices.Contains(leftOrder[slot]);
        public bool IsRightMatched(int slot) => matchedPairIndices.Contains(rightOrder[slot]);

        public bool IsLeftSelected(int slot) => selectedLeftSlot == slot;
        public bool IsRightSelected(int slot) => selectedRightSlot == slot;

        /// <summary>Index into a shared color palette, stable per pair, for coloring matched pairs distinctly.</summary>
        public int GetLeftColorIndex(int slot) => leftOrder[slot];
        public int GetRightColorIndex(int slot) => rightOrder[slot];

        public override void StartRound()
        {
            currentSet = availableSets.FirstOrDefault(s => s.category == currentCategory && s.tier == currentTier);
            if (currentSet == null || currentSet.pairs.Count == 0)
            {
                Debug.LogError($"No match-pair set found for aihe={currentCategory}, tier={currentTier}. " +
                                "Check that the set was imported and is assigned in the inspector.");
                leftOrder = new List<int>();
                rightOrder = new List<int>();
                return;
            }

            var allIndices = Enumerable.Range(0, currentSet.pairs.Count).ToList();

            // If a smaller round size was requested and there are more pairs available than that,
            // sample a subset; otherwise use every pair in the set.
            var chosen = (targetQuestionCount > 0 && targetQuestionCount < allIndices.Count)
                ? Shuffle(allIndices).Take(targetQuestionCount).ToList()
                : allIndices;

            leftOrder = Shuffle(chosen);
            rightOrder = Shuffle(chosen);

            matchedPairIndices.Clear();
            selectedLeftSlot = null;
            selectedRightSlot = null;
            HasPendingAttempt = false;

            correctCount = 0;
            totalAnswered = 0;
        }

        public void SelectLeft(int slot)
        {
            if (IsLeftMatched(slot)) return;
            selectedLeftSlot = slot;
            TryResolve();
        }

        public void SelectRight(int slot)
        {
            if (IsRightMatched(slot)) return;
            selectedRightSlot = slot;
            TryResolve();
        }

        private void TryResolve()
        {
            if (selectedLeftSlot == null || selectedRightSlot == null) return;

            int leftPair = leftOrder[selectedLeftSlot.Value];
            int rightPair = rightOrder[selectedRightSlot.Value];

            bool correct = leftPair == rightPair;
            LastAttemptWasCorrect = correct;
            HasPendingAttempt = true;
            totalAnswered++;

            if (correct)
            {
                matchedPairIndices.Add(leftPair);
                correctCount++;
            }

            selectedLeftSlot = null;
            selectedRightSlot = null;
        }

        /// <summary>Call from the view once it has shown/consumed the correct/incorrect flash for the last attempt.</summary>
        public void ClearPendingAttempt() => HasPendingAttempt = false;

        /// <summary>Unused directly -- selection is handled via SelectLeft/SelectRight instead of a single answer object.</summary>
        public override void EvaluateAnswer(object answer) { }

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
