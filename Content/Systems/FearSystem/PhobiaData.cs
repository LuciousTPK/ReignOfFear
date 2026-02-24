using System.Collections.Generic;
using Terraria.ID;

namespace ReignOfFear.Content.Systems.FearSystem
{
    /// <summary>
    /// This class is used to create instances of static phobia data
    /// It is used to define a phobia's type, their gauges, and their different thresholds
    /// It also assigns them to a phobia set
    /// </summary>

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

    }

    /// <summary>
    /// This class is used to tie the actual PhobiaID of a phobia (found in FearSystemPlayer.cs)
    /// to a newly defined instance of PhobiaDefinition
    /// 
    /// In essence, this is where you define/create phobias
    /// 
    /// This is also where we map enemies/projectiles to phobias as well
    /// using their PhobiaIDs
    /// </summary>

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
