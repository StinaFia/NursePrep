using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NursingQuiz.GameModes.MultipleChoice
{
    /// <summary>
    /// References the UI elements used by a single answer option.
    ///
    /// MultipleChoiceView uses this component to update the option text, selection state and visual styling.
    /// </summary>
    public class AnswerOptionUI : MonoBehaviour
    {
        [Tooltip("The Button component on this same GameObject.")]
        public Button Button;

        [Tooltip("The Image used as this button's background/fill (for selected/correct/incorrect coloring).")]
        public Image Background;

        [Tooltip("The TextMeshPro label showing the option's text (A/B/C/D content).")]
        public TMP_Text Label;
    }
}
