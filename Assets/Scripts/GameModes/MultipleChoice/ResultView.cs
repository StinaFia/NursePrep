using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NursingQuiz.GameModes.MultipleChoice
{
    /// <summary>
    /// Displays the round completion summary.
    ///
    /// Shows:
    /// - selected theme
    /// - score
    /// - star rating
    /// - performance feedback message
    ///
    /// The view only handles presentation and does not contain any gameplay or scoring logic.
    /// </summary>
    public class ResultView : MonoBehaviour
    {
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text resultFeedbackText;
        [SerializeField] private TMP_Text themeText;
        [SerializeField] private Image[] stars;
        [SerializeField] private Color gold = new Color(0.88f, 0.65f, 0.23f);
        [SerializeField] private Color empty = new Color(0.85f, 0.85f, 0.85f);

        /// <summary>
        /// Updates the result screen using the round outcome.
        ///
        /// Star rating and feedback text are derived from the player's score percentage.
        /// </summary>
        public void Show(int correct, int total, string theme)
        {
            themeText.text = theme;
            scoreText.text = $"{correct} / {total}";

            // Calculate how many stars should be highlighted based on the percentage of correct answers.
            int filled = Mathf.RoundToInt((float)correct / total * stars.Length);

            // Highlight earned stars and visually de-emphasize unearned stars by reducing their size.
            for (int i = 0; i < stars.Length; i++)
            {
                bool earned = i < filled;

                stars[i].color = earned ? gold : empty;

                stars[i].rectTransform.localScale =
                earned
                ? Vector3.one
                : Vector3.one * 0.6f;
            }

            // Display a result message based on score percentage.
            float ratio = (float)correct / total;

            if (ratio >= 0.9f)
                resultFeedbackText.text = "Erinomainen suoritus! Osaat tämän aiheen hyvin";
            else if (ratio >= 0.6f)
                resultFeedbackText.text = "Hyvä suoritus. Hallitset jo perusteita, mutta harjoittelu vahvistaa osaamista";
            else
                resultFeedbackText.text = "Hyvä alku, kertaa aihe vielä ja kokeile uudelleen";
        }
    }
}