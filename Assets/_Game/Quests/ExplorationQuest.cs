namespace Ecos
{
    public static class ExplorationQuest
    {
        public static string Objective(SaveData state)
        {
            if (state.rewarded)
                return "O vale voltou a cantar. Encontre os baús selados e registre os fragmentos antigos.";
            if (!state.campBuilt)
                return "Um refúgio no vale — acenda a fogueira.\nReúna 4 madeiras e 3 pedras perto do acampamento.";
            if (!state.lensCrafted)
                return "Olhar além da névoa — crie a lente na bancada.\nReúna 2 cristais, 3 ervas e 2 pedras. A lente revela ecos com Q.";
            if (ProgressionRules.CompletedShrines(state) == 3)
                return "As três vozes foram restauradas. Libere espaço no inventário para receber o Artefato do Vale.";
            return "Vozes do Vale — restaure os três santuários (" + ProgressionRules.CompletedShrines(state)
                + "/3).\nCada altar usa 1 cristal e 2 recursos temáticos. Use Q para procurar ecos.";
        }

        public static bool Evaluate(SaveData state) => ProgressionRules.CompleteQuest(state);
    }
}
