namespace Ecos.Haunt
{
    /// <summary>Damage protection covers the first 60% of a dodge, not its recovery.</summary>
    public static class DodgeRules
    {
        public static bool IsInvulnerable(float remaining, float duration)
        {
            if (float.IsNaN(remaining) || float.IsInfinity(remaining)
                || float.IsNaN(duration) || float.IsInfinity(duration)
                || duration <= 0 || remaining <= 0 || remaining > duration) return false;
            return remaining > duration * .4f;
        }
    }
}
