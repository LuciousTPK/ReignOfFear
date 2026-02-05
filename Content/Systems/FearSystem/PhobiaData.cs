using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace ReignOfFear.Content.Systems.FearSystem
{
    public class PhobiaDefinition
    {
        public enum PhobiaType
        {
            Enemy,
            Boss,
            Biome,
            Event,
            Debuff,
            Enviromental,
            Other
        }

        public PhobiaType type;

        public PhobiaSet set;

        public int preAcquisitionMax;
        public int postAcquisitionMax;
        public int courageMax;
        public int conquerThreshold;
        public int rank2Threshold;
        public int rank3Threshold;

        public int CalculateRank(int fearPoints, int currentRank, bool rankIncrease)
        {
            // Due to hysteresis the scope of Rank 2 and Rank 3 overlap between their respective thresholds
            // There is also an overlap in scope for Rank 1 and Rank 2 between 0 and Rank 2's threshold
            // So long as the current rank falls within these scopes, we can safetly return current rank
            // Otherwise if current rank is an opposing number to the overlapped scopes, we default to Rank 2
            // Since such an increase guarentees that Rank 2's threshold is crossed due to Rank 2's scope spanning both overlaps

            if (rankIncrease)
            {
                if (fearPoints >= postAcquisitionMax) return 4; // For burdens since there is obviously no numeric in the name
                if (fearPoints >= rank3Threshold) return 3; // No rank overlap between max fear and 3rd rank threshold -- always Rank 3

                if (fearPoints < rank3Threshold && fearPoints >  rank2Threshold)
                {
                    switch (currentRank)
                    {
                        case 1:
                        {
                            return 2;
                        }

                        default:
                        {
                            return currentRank;
                        }
                    }
                }

                if (fearPoints == rank2Threshold) return 2; // Thresholds have no overlap, they are absolute and always return their respective rank

                // Since we are increasing the player can never achieve 0, thus we only check for an opposing current rank -- default return is 2
                if (fearPoints < rank2Threshold && currentRank != 3)
                {
                    return currentRank;
                }

                return 2;
            }

            else
            {
                if (fearPoints == 0) return 1; // Similar to max fear returning 4 for burden, 0 always returns 1

                if (fearPoints > 0 && fearPoints < rank2Threshold)
                {
                    switch (currentRank)
                    {
                        case 3:
                        {
                            return 2;
                        }

                        default:
                        {
                            return currentRank;
                        }
                    }
                }

                if (fearPoints == rank2Threshold) return 2; // Thresholds have no overlap, they are absolute and always return their respective rank

                // Unlike when fear points is increasing, we must use a secondary switch case for decreasing fear points
                // This is because 0 is a hard limit that can't be achieved via increasing fear points, so the final fear point check has a 100% chance of returning true
                // This requires utilizing the opposing rank of the overlap as a direct check instead and using it's return value as the default for scenarios where it's false
                // Since rank3Threshold has no such hard limit, we have to do a proper check for this gap -- however, since there is no overlaps beyond Rank 3's threshold
                // We can safely default to 3 if this check returns false
                if (fearPoints > rank2Threshold && fearPoints < rank3Threshold)
                {
                    switch (currentRank)
                    {
                        case 1:
                        {
                            return 2;
                        }

                        default:
                        {
                            return currentRank;
                        }
                    }
                }

                return 3;
            }
        }
    }
    public static class PhobiaData
    {
        public static Dictionary<PhobiaID, PhobiaDefinition> Definitions = new Dictionary<PhobiaID, PhobiaDefinition>
        {
            { PhobiaID.Kinemortophobia, new PhobiaDefinition { type = PhobiaDefinition.PhobiaType.Enemy, preAcquisitionMax = 300, postAcquisitionMax = 300, courageMax = 150, conquerThreshold = 100, rank2Threshold = 100, rank3Threshold = 200 } },
            { PhobiaID.Skelephobia, new PhobiaDefinition { type = PhobiaDefinition.PhobiaType.Enemy, preAcquisitionMax = 100, postAcquisitionMax = 600, courageMax = 300, conquerThreshold = 250, rank2Threshold = 200, rank3Threshold = 400 } },
            { PhobiaID.Phasmophobia, new PhobiaDefinition { type = PhobiaDefinition.PhobiaType.Enemy, preAcquisitionMax = 200, postAcquisitionMax = 900, courageMax = 450, conquerThreshold = 400, rank2Threshold = 300, rank3Threshold = 600 } }
        };

        public static Dictionary<int, List<PhobiaID>> NPCPhobiaMap = new Dictionary<int, List<PhobiaID>>
        {
            { NPCID.Zombie, new List<PhobiaID> { PhobiaID.Kinemortophobia } },
            { NPCID.ZombieRaincoat, new List<PhobiaID> { PhobiaID.Kinemortophobia } },
            { NPCID.VortexHornet, new List<PhobiaID> { PhobiaID.Kinemortophobia } },
            { NPCID.SolarDrakomire, new List<PhobiaID> { PhobiaID.Kinemortophobia } },
            { NPCID.GoblinScout, new List<PhobiaID> { PhobiaID.Kinemortophobia } },
            { NPCID.BigZombie, new List<PhobiaID> { PhobiaID.Kinemortophobia } },
            { NPCID.Skeleton, new List<PhobiaID> { PhobiaID.Kinemortophobia } },
            { NPCID.DungeonSpirit, new List<PhobiaID> { PhobiaID.Kinemortophobia } },
            { NPCID.SkeletronPrime, new List<PhobiaID> { PhobiaID.Kinemortophobia } },
            { NPCID.PrimeCannon, new List<PhobiaID> { PhobiaID.Kinemortophobia } },
            { NPCID.PrimeLaser, new List<PhobiaID> { PhobiaID.Kinemortophobia } },
            { NPCID.PrimeSaw, new List<PhobiaID> { PhobiaID.Kinemortophobia } },
            { NPCID.PrimeVice, new List<PhobiaID> { PhobiaID.Kinemortophobia } },
            { NPCID.TheDestroyer, new List<PhobiaID> { PhobiaID.Kinemortophobia } },
            { NPCID.TheDestroyerBody, new List<PhobiaID> { PhobiaID.Kinemortophobia } },
            { NPCID.TheDestroyerTail, new List<PhobiaID> { PhobiaID.Kinemortophobia } },
            { NPCID.SkeletronHead, new List<PhobiaID> { PhobiaID.Kinemortophobia } },
            { NPCID.SkeletronHand, new List<PhobiaID> { PhobiaID.Kinemortophobia } },
            { NPCID.GiantWormBody, new List<PhobiaID> { PhobiaID.Kinemortophobia } },
            { NPCID.GiantWormHead, new List<PhobiaID> { PhobiaID.Kinemortophobia } },
            { NPCID.GiantWormTail, new List<PhobiaID> { PhobiaID.Kinemortophobia } },
        };
    }
}
