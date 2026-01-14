using System.Collections.Generic;
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
            { NPCID.BigZombie, new List<PhobiaID> { PhobiaID.Kinemortophobia } },
            { NPCID.Skeleton, new List<PhobiaID> { PhobiaID.Skelephobia } },
            { NPCID.DungeonSpirit, new List<PhobiaID> { PhobiaID.Phasmophobia } },
        };
    }
}
