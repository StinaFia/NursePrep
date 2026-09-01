using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NursingQuiz.UI
{
    /// <summary>
    /// Attached to the theme-button template under the combined theme+difficulty panel.
    /// GameFlowController clones one of these per ThemeInfo asset it's given, so new themes
    /// appear automatically just by dragging their asset into GameFlowController's Available
    /// Themes list -- no new buttons need to be hand-built as content grows.
    /// </summary>
    public class ThemeButtonUI : MonoBehaviour
    {
        public Button Button;
        public TMP_Text Label;

        [Tooltip("Background image used to visually highlight the currently selected theme.")]
        public Image Background;

        /// <summary>Set at spawn time by GameFlowController -- which theme this button represents.</summary>
        [HideInInspector] public string Aihe;

        [HideInInspector] public string Aihekori;
    }
}
