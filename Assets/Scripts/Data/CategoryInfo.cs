using UnityEngine;

namespace NursingQuiz.UI
{
    /// <summary>
    /// Configuration data for a top-level category shown in the Category Selection screen.
    ///
    /// Used by GameFlowController to build the category list UI.
    /// </summary>
    [System.Serializable]
    public class CategoryInfo
    {
        public string category;
        public string displayName;
        [TextArea] public string description;
    }
}