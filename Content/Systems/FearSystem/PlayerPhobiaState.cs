using System.Collections.Generic;

namespace ReignOfFear.Content.Systems.FearSystem
{
    internal class PlayerPhobiaState
    {
        public float fearPoints;
        public bool hasPhobia;
        public bool isImmune;
        public bool isBurden;
        public int currentRank;
        public int maxFear = 300; // Temporary placeholder
        public List<PhobiaDebuff> activeDebuffs = new List<PhobiaDebuff>();
    }
}
