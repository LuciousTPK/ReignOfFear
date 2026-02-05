using System.Collections.Generic;

namespace ReignOfFear.Content.Systems.FearSystem
{
    public static class SegmentedBossData
    {
        private static readonly HashSet<int> AI1_GroupedTypes = new HashSet<int>
        {
            35, 36,
            127, 128, 129, 130, 131
        };

        public static bool UsesAI1Grouping(int npcType)
        {
            return AI1_GroupedTypes.Contains(npcType);
        }
    }
}