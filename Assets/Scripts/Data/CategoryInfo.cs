using UnityEngine;

namespace NursingQuiz.UI
{
    [System.Serializable]
    public class CategoryInfo
    {
        public string category;
        public string displayName;
        [TextArea] public string description;
    }
}