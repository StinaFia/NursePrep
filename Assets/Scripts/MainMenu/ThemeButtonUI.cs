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
    ///
    /// Shows the theme's Oppimistavoite directly on the button (not behind a popup), so the
    /// player sees the objective for every theme at a glance before choosing.
    /// </summary>
    public class ThemeButtonUI : MonoBehaviour
    {
        public Button Button;
        public TMP_Text Label;
        public Image Background;

        [Tooltip("Shows the theme's Oppimistavoite text directly on the button.")]
        public TMP_Text ObjectiveText;

        /// <summary>Set at spawn time by GameFlowController -- which theme this button represents.</summary>
        [HideInInspector] public string Aihe;

        [HideInInspector] public string Aihekori;
    }
}
