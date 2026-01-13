using System.Collections.Generic;
using Terraria.ID;

namespace ReignOfFear.Content.Systems.FearSystem
{
    public static class PhobiaData
    {
        public static Dictionary<int, List<PhobiaID>> NPCPhobiaMap = new Dictionary<int, List<PhobiaID>>
        {
            { NPCID.Zombie, new List<PhobiaID> { PhobiaID.Kinemortophobia } },
            { NPCID.ZombieRaincoat, new List<PhobiaID> { PhobiaID.Kinemortophobia } },
            { NPCID.BigZombie, new List<PhobiaID> { PhobiaID.Kinemortophobia } },
        };
    }
}
