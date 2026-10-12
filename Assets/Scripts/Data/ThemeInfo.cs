using UnityEngine;

namespace NursingQuiz.Data
{
    /// <summary>
    /// Stores information shared by all difficulty tiers of a single category + theme combination.
    ///
    /// Used by the theme selection screen to display the theme name and learning objective.
    ///
    /// The importer creates or updates one ThemeInfo asset per category-theme combination.
    /// </summary>
    public class ThemeInfo : ScriptableObject
    {
        // Category identifier (for example LAA or LAS).
        public string category;
        // Theme identifier within the category (for example PER, TUR or YKM).
        public string theme;

        [Tooltip("Player-facing name for this theme. Only auto-filled if empty -- edit freely.")]
        public string displayName;

        [Tooltip("This theme's learning objective (Oppimistavoite), shown once when the theme is selected.")]
        [TextArea(2, 6)]
        public string oppimistavoite;
    }
}
