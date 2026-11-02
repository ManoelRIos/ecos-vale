using System;

namespace Ecos
{
    /// <summary>Stable species identifiers and deterministic habitat selection, independent of Unity.</summary>
    public static class CreatureCatalog
    {
        public static readonly string[] SpeciesIds = { "hare", "fox", "deer", "boar", "bat", "wisp" };

        public static bool IsSpecies(string id) => Array.IndexOf(SpeciesIds, id) >= 0;
        public static bool IsNocturnal(string id) => id == "bat" || id == "wisp";
        public static string Name(string id)
        {
            switch (id)
            {
                case "hare": return "Coelho das flores";
                case "fox": return "Raposa de musgo";
                case "deer": return "Cervo das lanternas";
                case "boar": return "Javali das pedras";
                case "bat": return "Morcego violeta";
                case "wisp": return "Fagulha do vale";
                default: return "Criatura desconhecida";
            }
        }

        public static string Description(string id)
        {
            switch (id)
            {
                case "hare": return "Pequeno e atento, salta entre flores. Corra perto dele e fugirá; agache-se para observar de perto.";
                case "fox": return "Uma raposa de cauda verde que investiga viajantes tranquilos. Aproxima-se por curiosidade, mas recua diante de movimentos bruscos.";
                case "deer": return "Os chifres dourados lembram pequenas lanternas. Pasta em clareiras e foge de passos apressados; a aproximação agachada é mais discreta.";
                case "boar": return "Fareja o chão e para para encarar intrusos. Apesar da aparência robusta, prefere se afastar de uma corrida a lutar.";
                case "bat": return "Sai ao anoitecer e voa baixo em círculos. Suas asas violetas cruzam as trilhas sem ameaçar o viajante.";
                case "wisp": return "Uma luz viva que desperta durante a noite. A lente de ressonância atrai sua curiosidade, permitindo observá-la de perto.";
                default: return "Esta espécie ainda não foi identificada.";
            }
        }

        public static string ChooseSpecies(int cellX, int cellZ, int seed, int biome, bool night)
        {
            uint habitat = WorldMath.Hash(cellX, cellZ, seed + 1877);
            if (night && habitat % 5 < 2) return habitat % 2 == 0 ? "bat" : "wisp";
            int choice = (int)(habitat % 8);
            // Every habitat admits visitors, while its common residents differ by biome.
            if (biome == 2) return choice < 4 ? "hare" : choice < 6 ? "deer" : "fox";
            if (biome == 3 || biome == 5) return choice < 4 ? "boar" : choice < 6 ? "fox" : choice == 6 ? "hare" : "deer";
            return choice < 2 ? "hare" : choice < 4 ? "fox" : choice < 7 ? "deer" : "boar";
        }
    }
}
