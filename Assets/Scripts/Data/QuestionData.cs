using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NursingQuiz.Core;

namespace NursingQuiz.Data
{
    /// <summary>
    /// Represents a single multiple-choice question.
    ///
    /// QuestionData assets are generated and updated by QuestionBankImporter from CSV source files.
    /// Changes should be made in the source CSV and imported again rather than editing assets manually.
    /// </summary>
    public class QuestionData : ScriptableObject
    {
        [Header("Identity")]
        /// <summary>
        /// Unique question identifier.
        /// Example: LAA-PER-001
        /// </summary>
        public string tunniste;
        /// <summary>
        /// Category identifier.
        /// Example: LAAK or LASK.
        /// </summary>
        public string category;
        /// <summary>
        /// Theme identifier within the category.
        /// Example: PER, TUR or YKM.
        /// </summary>
        public string theme; 
        public DifficultyTier tier;

        [Header("Content")]
        [TextArea(2, 5)] public string questionText;
        /// <summary>
        /// Answer option texts in display order (A-D).
        /// </summary>
        public List<string> options = new List<string>();

        [TextArea(1, 4)] public string infoBubble;

        [Tooltip("0-based indices into 'options' that are correct. Empty list = no correct answer (T3 only).")]
        public List<int> correctIndices = new List<int>();

        [Header("Feedback")]
        [TextArea(1, 3)] public string correctFeedback;
        [TextArea(1, 3)] public string incorrectFeedback;
        [TextArea(2, 5)] public string rationale;
        [TextArea(1, 4)] public string professionalPatch;

        /// <summary>
        /// Determines whether the UI should allow selecting multiple answer options.
        ///
        /// This is based on difficulty tier rather than the number of correct answers to avoid revealing
        /// answer count information to the player.
        /// </summary>
        public bool AllowsMultiSelect => tier != DifficultyTier.T1;

        /// <summary>
        /// Compares the player's selection against the set of correct answer indices.
        /// </summary>
        public bool IsAnswerCorrect(IEnumerable<int> selectedIndices)
        {
            var selected = new HashSet<int>(selectedIndices);
            var correct = new HashSet<int>(correctIndices);
            return selected.SetEquals(correct);
        }
    }
}
