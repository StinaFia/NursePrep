using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NursingQuiz.Core;

namespace NursingQuiz.Data
{
    /// <summary>
    /// A single multiple-choice question. Instances are created/updated by QuestionBankImporter --
    /// do not edit these assets by hand. Edit the source CSV and re-run the importer instead.
    /// </summary>
    public class QuestionData : ScriptableObject
    {
        [Header("Identity")]
        public string tunniste;   // e.g. LAA-PER-001
        public string category;       // e.g. LAA
        public string theme;   // e.g. PER
        public DifficultyTier tier;

        [Header("Content")]
        [TextArea(2, 5)] public string questionText;
        public List<string> options = new List<string>(); // A, B, C, D in order

        [Tooltip("0-based indices into 'options' that are correct. Empty list = no correct answer (T3 only).")]
        public List<int> correctIndices = new List<int>();

        [Header("Feedback")]
        [TextArea(1, 3)] public string correctFeedback;
        [TextArea(1, 3)] public string incorrectFeedback;
        [TextArea(2, 5)] public string rationale;
        [TextArea(1, 4)] public string professionalPatch;

        /// <summary>
        /// Whether the answer UI should allow selecting more than one option.
        /// This is tier-based, not based on how many answers actually happen to be correct --
        /// T2/T3 must always present as multi-select, otherwise a single-select UI would give
        /// away the correct-answer count before the student answers.
        /// </summary>
        public bool AllowsMultiSelect => tier != DifficultyTier.T1;

        public bool IsAnswerCorrect(IEnumerable<int> selectedIndices)
        {
            var selected = new HashSet<int>(selectedIndices);
            var correct = new HashSet<int>(correctIndices);
            return selected.SetEquals(correct);
        }
    }
}
