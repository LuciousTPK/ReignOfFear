using System;
using System.Collections.Generic;
using System.Linq;
using static ReignOfFear.Content.Systems.FearSystem.PhobiaDebuff;

namespace ReignOfFear.Content.Systems.FearSystem
{
    public class PhobiaDebuff
    {
        public enum PhobiaDebuffID
        {
            // Enemy Phobia Rank 2
            TraumaticStrike,
            TerrorRadius,
            FearToxin,
            CalledByTheGrave,

            // Enemy Phobia Rank 3
            DarkCovenant,
            HaroldsOfDoom,
            SpectralChain,
            BlessedByEvil,
        }

        public PhobiaDebuffID id;
        public PhobiaDefinition.PhobiaType type;
        public int rank;
    }
    public static class PhobiaDebuffData
    {
        public static PhobiaDebuff SelectDebuff(PhobiaID phobia, int rank)
        {
            PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition definition);
            PhobiaDefinition.PhobiaType phobiaType = definition.type;

            List<PhobiaDebuff> typeList = typeDebuffs[phobiaType];
            List<PhobiaDebuff> phobiaSpecificList = phobiaSpecificDebuffs[phobia];

            List<PhobiaDebuff> typeFiltered = typeList.Where(debuff => debuff.rank == rank).ToList();
            List<PhobiaDebuff> phobiaSpecificFiltered = phobiaSpecificList.Where(debuff => debuff.rank == rank).ToList();

            List<PhobiaDebuff> combinedList = typeFiltered.Concat(phobiaSpecificFiltered).ToList();
            return combinedList[Random.Shared.Next(0, combinedList.Count)];
        }

        public static Dictionary<PhobiaDefinition.PhobiaType, List<PhobiaDebuff>> typeDebuffs = new Dictionary<PhobiaDefinition.PhobiaType, List<PhobiaDebuff>>
        {
            {
                PhobiaDefinition.PhobiaType.Enemy,
                new List<PhobiaDebuff>
                {
                    new PhobiaDebuff { id = PhobiaDebuffID.TerrorRadius, type = PhobiaDefinition.PhobiaType.Enemy, rank = 2 },
                    new PhobiaDebuff { id = PhobiaDebuffID.TraumaticStrike, type = PhobiaDefinition.PhobiaType.Enemy, rank = 2 },
                }
            }
        };

        public static Dictionary<PhobiaID, List<PhobiaDebuff>> phobiaSpecificDebuffs = new Dictionary<PhobiaID, List<PhobiaDebuff>>
        {
            {
                PhobiaID.Kinemortophobia,
                new List<PhobiaDebuff>
                {
                    
                }
            }
        };
    }
}
