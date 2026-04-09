using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace ReignOfFear.Content.Systems.FearSystem
{
    public class AIGroupConfig
    {
        public int AIIndex;
        public HashSet<int> AllTypes;
        public HashSet<int> PrimaryTypes;
    }

    // ***IMPORTANT**
    // NEED TO LOOK OVER VANILLA BOSSES/ENEMIES TO IDENTIFY SERVANT TYPE ENEMIES AND TIE THEM TOGETHER AS A SINGULAR
    // COMBAT INSTANCE. THIS FILE IS INCOMPLETE, THIS LOGIC CURRENTLY JUST TRACKS BOSSES/ENEMIES ON THEIR OWN,
    // NOT ENEMIES THAT ARE UNIQUE TO THE BOSS/ENEMY (IGNORE IF BOSS/ENEMY IS NOT UNIQUE TO THE ENEMY/BOSS IN QUESTION)!

    /// <summary>
    /// This is a helper class used to store the methods used to determine what a 'combat instance' is
    /// when talking about segmented enemies/bosses or bosses that in one way or another
    /// encompass multiple NPCs that need to be tracked a singular combat instance
    /// 
    /// Ex. NPCs that are required to be destroyed for the main boss to die, NPCs that are directly a part of the boss,
    /// or NPCs that all share one health bar (I.E. their 'realLife' values match)
    /// </summary>

    public static class SegmentedBossData
    {
        /// <remarks>
        /// The bread and butter of this class is creating combat keys based on the whoAmI value of the enemy in
        /// relation to the combat instance initialization. We do this because 'whoAmI' is always guaranteed to be
        /// unique to that singular entity and is also the value that ai[] and 'realLife' uses to define itself
        /// 
        /// In vanilla Terraria there are 21 segmented enemies total, with 12 of those enemies being singular entities
        /// composed of multiple NPCs that share a health bar, 6 of them being singular entities composed of multiple NPCs
        /// that share separate health bars, and finally 3 of them being special cases a battle is composed of multiple separate
        /// enemies
        /// 
        /// To track the first 12 we simply treat their 'realLife' value as the combat key since they derive the value from
        /// the head/core's whoAmI value, which works perfectly for our purposes. For the next 6 we refer to some specific index
        /// in their ai[] values to determine how the parts track the head/core of the enemy, which is also derived from it's whoAmI
        /// value. Finally, for the final 3 enemies special case by case logic was created in order to identify what an 'instance' of
        /// combat means for them
        /// </remarks>
        private static Dictionary<int, AIGroupConfig> AIGroupedBosses = new Dictionary<int, AIGroupConfig>();
        private static Dictionary<int, int> npcToCombatKey = new Dictionary<int, int>();

        private static readonly HashSet<int> TwinTypes = new HashSet<int> { NPCID.Retinazer, NPCID.Spazmatism };
        private static readonly HashSet<int> GolemTypes = new HashSet<int> { NPCID.Golem, NPCID.GolemHead, NPCID.GolemFistLeft, NPCID.GolemFistRight, NPCID.GolemHeadFree };
        private static readonly HashSet<int> EaterTypes = new HashSet<int> { NPCID.EaterofWorldsHead, NPCID.EaterofWorldsBody, NPCID.EaterofWorldsTail };
        private static readonly HashSet<int> BrainTypes = new HashSet<int> { NPCID.BrainofCthulhu, NPCID.Creeper };
        private static readonly HashSet<int> PlanteraTypes = new HashSet<int> { NPCID.Plantera, NPCID.PlanterasHook };

        static SegmentedBossData()
        {
            var skeletronConfig = new AIGroupConfig
            {
                AIIndex = 1,
                AllTypes = new HashSet<int> { NPCID.SkeletronHead, NPCID.SkeletronHand },
                PrimaryTypes = new HashSet<int> { NPCID.SkeletronHead }
            };
            foreach (int type in skeletronConfig.AllTypes)
                AIGroupedBosses[type] = skeletronConfig;

            var primeConfig = new AIGroupConfig
            {
                AIIndex = 1,
                AllTypes = new HashSet<int> { NPCID.SkeletronPrime, NPCID.PrimeCannon, NPCID.PrimeSaw, NPCID.PrimeVice, NPCID.PrimeLaser },
                PrimaryTypes = new HashSet<int> { NPCID.SkeletronPrime }
            };
            foreach (int type in primeConfig.AllTypes)
                AIGroupedBosses[type] = primeConfig;

            var pumpkingConfig = new AIGroupConfig
            {
                AIIndex = 1,
                AllTypes = new HashSet<int> { NPCID.Pumpking, NPCID.PumpkingBlade },
                PrimaryTypes = new HashSet<int> { NPCID.Pumpking }
            };
            foreach (int type in pumpkingConfig.AllTypes)
                AIGroupedBosses[type] = pumpkingConfig;

            var dutchmanConfig = new AIGroupConfig
            {
                AIIndex = 0,
                AllTypes = new HashSet<int> { NPCID.PirateShip, NPCID.PirateShipCannon },
                PrimaryTypes = new HashSet<int> { NPCID.PirateShip }
            };
            foreach (int type in dutchmanConfig.AllTypes)
                AIGroupedBosses[type] = dutchmanConfig;

            var saucerConfig = new AIGroupConfig
            {
                AIIndex = 0,
                AllTypes = new HashSet<int> { NPCID.MartianSaucer, NPCID.MartianSaucerTurret, NPCID.MartianSaucerCannon, NPCID.MartianSaucerCore },
                PrimaryTypes = new HashSet<int> { NPCID.MartianSaucerCore }
            };
            foreach (int type in saucerConfig.AllTypes)
                AIGroupedBosses[type] = saucerConfig;

            var moonLordConfig = new AIGroupConfig
            {
                AIIndex = 3,
                AllTypes = new HashSet<int> { NPCID.MoonLordHead, NPCID.MoonLordHand, NPCID.MoonLordCore },
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

        public static bool IsGolemType(int npcType) => GolemTypes.Contains(npcType);

        public static bool IsBrainType(int npcType) => BrainTypes.Contains(npcType);

        public static bool IsPlanteraType(int npcType) => PlanteraTypes.Contains(npcType);

        public static bool TryGetGolemCombatKey(Dictionary<int, CombatData> activeCombats, out int existingKey)
        {
            int whoAmI = NPC.golemBoss;
            if (whoAmI >= 0 && whoAmI < Main.maxNPCs && activeCombats.ContainsKey(whoAmI))
            {
                existingKey = whoAmI;
                return true;
            }
            existingKey = -1;
            return false;
        }

        public static bool TryGetBrainCombatKey(Dictionary<int, CombatData> activeCombats, out int existingKey)
        {
            int whoAmI = NPC.crimsonBoss;
            if (whoAmI >= 0 && whoAmI < Main.maxNPCs && activeCombats.ContainsKey(whoAmI))
            {
                existingKey = whoAmI;
                return true;
            }
            existingKey = -1;
            return false;
        }

        public static bool TryGetPlanteraCombatKey(Dictionary<int, CombatData> activeCombats, out int existingKey)
        {
            int whoAmI = NPC.plantBoss;
            if (whoAmI >= 0 && whoAmI < Main.maxNPCs && activeCombats.ContainsKey(whoAmI))
            {
                existingKey = whoAmI;
                return true;
            }
            existingKey = -1;
            return false;
        }

        /// <remarks>
        /// These next five methods are the next grouping, all created to contend with The Twins. Despite
        /// how they have the fleshy tendril connecting them together, there is actually no logic that
        /// pairs the two together since they are technically their own bosses. There is merely logic
        /// that createes the tendril so long as an opposing instance of one of the twins is actually
        /// active, regardless of how many there actually are. For this reason, if you spawn a bunch,
        /// multiple tendrils will attach to one of the twins so long as they are an opposing twin
        /// 
        /// Ex. Spawn one Retinazer and five Spazmatisms and five tendrils will connect to the Retinazer
        /// 
        /// For this reason, we can actually haphazardly create pairs on a whim by checking all
        /// NPCs for the first instance of an opposing twin and pairing the two together as a combat
        /// instance. On the off chance that, for whatever reason, there is not an opposing twin,
        /// the twin that caused the initialization of the combat instance will be considered a solo
        /// instance
        /// </remarks>
        public static bool IsTwinType(int npcType)
        {
            return TwinTypes.Contains(npcType);
        }

        public static int GetOppositeTwin(int npcType)
        {
            if (npcType == NPCID.Retinazer) return NPCID.Spazmatism;
            if (npcType == NPCID.Spazmatism) return NPCID.Retinazer;
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

        /// <remarks>
        /// Finally, the remainder of the methods targets the Eater of Worlds. This is a special case,
        /// because while ai[] tracks what components a EoW component should follow and be followed by,
        /// it doesn't track the entire worm. For this reason, when combat gets initiated, we need to
        /// snapshot the entire worm so it can be tracked even as components break away and turn into
        /// their own worms. We do this by traversing the body from the segment that initiated combat
        /// in order to snapshot the eater inside an array and use it as reference for the combat tracker
        /// </remarks>
        public static bool IsEaterType(int npcType)
        {
            return EaterTypes.Contains(npcType);
        }

        public static List<int> TraverseEaterChain(int startWhoAmI)
        {
            List<int> chain = new List<int>();
            HashSet<int> visited = new HashSet<int>();

            int currentWhoAmI = startWhoAmI;

            while (currentWhoAmI >= 0 && currentWhoAmI < Main.maxNPCs)
            {
                if (visited.Contains(currentWhoAmI))
                {
                    break;
                }

                NPC segment = Main.npc[currentWhoAmI];
                if (!segment.active || !IsEaterType(segment.type))
                {
                    break;
                }

                chain.Add(currentWhoAmI);
                visited.Add(currentWhoAmI);

                if (segment.type == NPCID.EaterofWorldsHead)
                {
                    break;
                }

                currentWhoAmI = (int)segment.ai[1];
            }

            return chain;
        }
    }
}