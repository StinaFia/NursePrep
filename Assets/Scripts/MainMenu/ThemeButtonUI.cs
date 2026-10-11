using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NursingQuiz.UI
{
    /// <summary>
    /// UI component attached to the theme button template used in the Theme and Difficulty Panel.
    /// 
    /// GameFlowController creates instances of this button dynamically for each available ThemeInfo asset
    /// and populates its text fields and selection data.
    /// </summary>
    public class ThemeButtonUI : MonoBehaviour
    {
        public Button Button;
        public TMP_Text Label;
        public Image Background;

        [Tooltip("Displays the learning objective (Oppimistavoite) for this theme")]
        public TMP_Text ObjectiveText;

        /// <summary>
        /// Set at spawn time by GameFlowController -- which theme this button represents.
        /// </summary>
        [HideInInspector] public string Aihe;

        [HideInInspector] public string Aihekori;
    }
}
