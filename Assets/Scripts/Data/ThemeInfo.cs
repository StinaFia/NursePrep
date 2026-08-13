using UnityEngine;

namespace NursingQuiz.Data
{
    /// <summary>
    /// Theme-level info -- one per aihe+aihekori combination, independent of difficulty tier.
    /// Holds only what's genuinely shared across tiers (currently just the display name).
    /// The learning objective (Oppimistavoite) lives on QuestionBank instead, since it's
    /// written per-tier and can legitimately differ between T1/T2/T3 for the same theme.
    ///
    /// displayName is only set by the importer if it's still empty, so you can safely rename
    /// it in the Inspector for a nicer player-facing label without it being overwritten on
    /// the next import.
    /// </summary>
    public class ThemeInfo : ScriptableObject
    {
        public string aihe;
        public string aihekori;

        [Tooltip("Player-facing name for this theme. Only auto-filled if empty -- edit freely.")]
        public string displayName;
    }
}
