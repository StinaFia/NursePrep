using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NursingQuiz.GameModes.MultipleChoice
{
    /// <summary>
    /// Attach this to each answer-option GameObject (the one with the Button component).
    /// Bundles its Button, background Image, and label Text so MultipleChoiceView can
    /// control them directly instead of relying on GetComponentInChildren calls, which
    /// break silently if the hierarchy under the button changes shape.
    ///
    /// All three fields are on the SAME GameObject/its children -- just drag them in from
    /// the Inspector once you've built the button.
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
