using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NursingQuiz.GameModes.MatchPairs
{
    /// <summary>
    /// Attach to each left/right item button template. MatchPairView clones one of these
    /// per pair, for both the left and right columns -- same self-wiring pattern as
    /// AnswerOptionUI and ThemeButtonUI elsewhere in the project.
    /// </summary>
    public class MatchItemUI : MonoBehaviour
    {
        public Button Button;
        public Image Background;
        public TMP_Text Label;
    }
}
