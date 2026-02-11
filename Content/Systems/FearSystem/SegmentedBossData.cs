using System.Collections.Generic;

namespace ReignOfFear.Content.Systems.FearSystem
{
    public class AIGroupConfig
    {
        public int AIIndex;
        public HashSet<int> AllTypes;
        public HashSet<int> PrimaryTypes;
    }

    public static class SegmentedBossData
    {
        private static Dictionary<int, AIGroupConfig> AIGroupedBosses = new Dictionary<int, AIGroupConfig>();
        private static Dictionary<int, int> npcToCombatKey = new Dictionary<int, int>();
        private static readonly HashSet<int> TwinTypes = new HashSet<int> { 125, 126 };
        private static readonly HashSet<int> GolemTypes = new HashSet<int> { 245, 246, 247, 248, 249 };

        static SegmentedBossData()
        {
            var skeletronConfig = new AIGroupConfig
            {
                AIIndex = 1,
                AllTypes = new HashSet<int> { 35, 36 },
                PrimaryTypes = new HashSet<int> { 35 }
            };
            foreach (int type in skeletronConfig.AllTypes)
                AIGroupedBosses[type] = skeletronConfig;

            var primeConfig = new AIGroupConfig
            {
                AIIndex = 1,
                AllTypes = new HashSet<int> { 127, 128, 129, 130, 131 },
                PrimaryTypes = new HashSet<int> { 127 }
            };
            foreach (int type in primeConfig.AllTypes)
                AIGroupedBosses[type] = primeConfig;

            var pumpkingConfig = new AIGroupConfig
            {
                AIIndex = 1,
                AllTypes = new HashSet<int> { 327, 328 },
                PrimaryTypes = new HashSet<int> { 327 }
            };
            foreach (int type in pumpkingConfig.AllTypes)
                AIGroupedBosses[type] = pumpkingConfig;

            var dutchmanConfig = new AIGroupConfig
            {
                AIIndex = 0,
                AllTypes = new HashSet<int> { 491, 492 },
                PrimaryTypes = new HashSet<int> { 491 }
            };
            foreach (int type in dutchmanConfig.AllTypes)
                AIGroupedBosses[type] = dutchmanConfig;

            var saucerConfig = new AIGroupConfig
            {
                AIIndex = 0,
                AllTypes = new HashSet<int> { 392, 393, 394, 395 },
                PrimaryTypes = new HashSet<int> { 395 }
            };
            foreach (int type in saucerConfig.AllTypes)
                AIGroupedBosses[type] = saucerConfig;

            var moonLordConfig = new AIGroupConfig
            {
                AIIndex = 3,
                AllTypes = new HashSet<int> { 396, 397, 398 },
                PrimaryTypes = new HashSet<int> { 398 }
            };
            foreach (int type in moonLordConfig.AllTypes)
                AIGroupedBosses[type] = moonLordConfig;
        }

        public static bool UsesAIGrouping(int npcType, out int aiIndex)
        {
            if (AIGroupedBosses.TryGetValue(npcType, out AIGroupConfig config))
            {
                aiIndex = config.AIIndex;
                return true;
            }
            aiIndex = -1;
            return false;
        }

        public static bool IsAIPrimary(int npcType)
        {
            if (AIGroupedBosses.TryGetValue(npcType, out AIGroupConfig config))
            {
                return config.PrimaryTypes.Contains(npcType);
            }
            return false;
        }

        public static bool IsGolemType(int npcType)
        {
            return GolemTypes.Contains(npcType);
        }

        public static bool TryGetGolemCombatKey(Dictionary<int, CombatData> activeCombats, out int existingKey)
        {
            foreach (var kvp in activeCombats)
            {
                if (GolemTypes.Contains(kvp.Value.npcType))
                {
                    existingKey = kvp.Key;
                    return true;
                }
            }
            existingKey = -1;
            return false;
        }

        public static bool IsTwinType(int npcType)
        {
            return TwinTypes.Contains(npcType);
        }

        public static int GetOppositeTwin(int npcType)
        {
            if (npcType == 125) return 126;
            if (npcType == 126) return 125;
            return -1;
        }

        public static void RegisterPairing(int combatKey, int npcWhoAmI)
        {
            npcToCombatKey[npcWhoAmI] = combatKey;
        }

        public static bool IsNPCPaired(int npcWhoAmI, out int combatKey)
        {
            return npcToCombatKey.TryGetValue(npcWhoAmI, out combatKey);
        }

        public static void UnregisterPairing(int combatKey, CombatData combat)
        {
            foreach (int npcWhoAmI in combat.pairedNPCs)
            {
                npcToCombatKey.Remove(npcWhoAmI);
            }
        }
    }
}