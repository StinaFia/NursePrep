using System.Collections.Generic;
using UnityEngine;
using NursingQuiz.Core;
using NursingQuiz.Data;

namespace NursingQuiz.GameModes.MatchPairs
{
    /// <summary>
    /// TEMPORARY test-only bootstrap -- calls BeginRound automatically on Start() so you can
    /// verify the match-pairs screen works before it's wired into GameFlowController.
    /// Delete this once real menu integration exists (same role the old
    /// MultipleChoiceTestSceneBuilder played early on for the quiz screen).
    /// </summary>
    [RequireComponent(typeof(MatchPairView))]
    public class MatchPairTestBootstrap : MonoBehaviour
    {
        [SerializeField] private List<MatchPairSet> testSets = new List<MatchPairSet>();
        [SerializeField] private string testAihe = "LAA";
        [SerializeField] private DifficultyTier testTier = DifficultyTier.T1;

        private void Start()
        {
            if (testSets.Count == 0)
            {
                Debug.LogError("MatchPairTestBootstrap: drag your MatchPairSet asset(s) onto Test Sets in the Inspector.");
                return;
            }

            GetComponent<MatchPairView>().BeginRound(testSets, testAihe, testTier);
        }
    }
}
