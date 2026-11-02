namespace Ecos
{
    public static class ExplorationQuest
    {
        public static string Objective(SaveData s)
        {
            if (s.rewarded)
                return "O vale voltou a cantar. Explore os seis biomas.";
            return $"Vozes do Vale — restaure os três santuários ({s.restored.Count}/3).\nColete cristais luminosos e use E junto a cada altar.";
        }

        public static bool Evaluate(SaveData s)
        {
            if (s.rewarded || s.restored.Count < 3)
                return false;
            if (!s.inventory.Add("artifact", 1))
                return false;
            s.rewarded = true;
            return true;
        }
    }
}
