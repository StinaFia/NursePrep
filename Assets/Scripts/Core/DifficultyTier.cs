namespace NursingQuiz.Core
{
    /// <summary>
    /// Difficulty tiers as defined in "Ohje tietovisojen laatimiseen".
    /// T1: exactly one correct answer (recall).
    /// T2: one or more correct answers (applied professional reasoning).
    /// T3: one, several, or zero correct answers (clinical/experiential judgement).
    /// </summary>
    public enum DifficultyTier
    {
        T1,
        T2,
        T3
    }
}
