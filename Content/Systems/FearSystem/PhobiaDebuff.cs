using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace ReignOfFear.Content.Systems.FearSystem
{
    public class PhobiaDebuff
    {
        public enum PhobiaDebuffID
        {
            Gingivitis,
            StinkBrain,
            ConnectButNoInternet,
            NoBitches,
            Cringe,
            NoMoney,
            OneEarBudStoppedWorking,
            SleptOnTheWrongSideOfTheBed,
            Sadness,
            InsertBadThingHere
        }

        public PhobiaDebuffID id;
        public string effectDescription; // Stub for now
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
                    new PhobiaDebuff { id = PhobiaDebuff.PhobiaDebuffID.Gingivitis, effectDescription = "Your teeth hurt. -10% damage.", type = PhobiaDefinition.PhobiaType.Enemy, rank = 2 },
                    new PhobiaDebuff { id = PhobiaDebuff.PhobiaDebuffID.StinkBrain, effectDescription = "Brain fog. -15% movement speed.", type = PhobiaDefinition.PhobiaType.Enemy, rank = 2 },
                    new PhobiaDebuff { id = PhobiaDebuff.PhobiaDebuffID.ConnectButNoInternet, effectDescription = "Existential dread. -5% all stats.", type = PhobiaDefinition.PhobiaType.Enemy, rank = 2 },
                    new PhobiaDebuff { id = PhobiaDebuff.PhobiaDebuffID.NoBitches, effectDescription = "Crushing loneliness. -20% defense.", type = PhobiaDefinition.PhobiaType.Enemy, rank = 3 },
                    new PhobiaDebuff { id = PhobiaDebuff.PhobiaDebuffID.Cringe, effectDescription = "Remembering that thing you said. -25% damage.", type = PhobiaDefinition.PhobiaType.Enemy, rank = 3 },
                    new PhobiaDebuff { id = PhobiaDebuff.PhobiaDebuffID.NoMoney, effectDescription = "Checking bank account. -30% movement speed.", type = PhobiaDefinition.PhobiaType.Enemy, rank = 3 },
                    new PhobiaDebuff { id = PhobiaDebuff.PhobiaDebuffID.OneEarBudStoppedWorking, effectDescription = "Worst possible thing. -50% all stats.", type = PhobiaDefinition.PhobiaType.Enemy, rank = 4 }
                }
            }
        };

        public static Dictionary<PhobiaID, List<PhobiaDebuff>> phobiaSpecificDebuffs = new Dictionary<PhobiaID, List<PhobiaDebuff>>
        {
            {
                PhobiaID.Kinemortophobia,
                new List<PhobiaDebuff>
                {
                    new PhobiaDebuff { id = PhobiaDebuff.PhobiaDebuffID.SleptOnTheWrongSideOfTheBed, effectDescription = "Everything hurts. -12% defense.", type = PhobiaDefinition.PhobiaType.Enemy, rank = 2 },
                    new PhobiaDebuff { id = PhobiaDebuff.PhobiaDebuffID.Sadness, effectDescription = "Just sad. -20% all stats.", type = PhobiaDefinition.PhobiaType.Enemy, rank = 3 },
                    new PhobiaDebuff { id = PhobiaDebuff.PhobiaDebuffID.InsertBadThingHere, effectDescription = "The worst. -100% hope.", type = PhobiaDefinition.PhobiaType.Enemy, rank = 4 }
                }
            }
        };
    }
}
