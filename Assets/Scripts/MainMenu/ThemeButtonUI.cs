using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NursingQuiz.UI
{
    /// <summary>
    /// Attached to the theme-button template under ThemeSelectPanel. GameFlowController
    /// clones one of these per ThemeInfo asset it's given, so new themes appear automatically
    /// just by dragging their asset into GameFlowController's Available Themes list -- no new
    /// buttons need to be hand-built as content grows.
    /// </summary>
    public class ThemeButtonUI : MonoBehaviour
    {
        public Button Button;
        public TMP_Text Label;
    }
}
