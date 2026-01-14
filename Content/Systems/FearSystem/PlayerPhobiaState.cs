using System.Collections.Generic;

namespace ReignOfFear.Content.Systems.FearSystem
{
    internal class PlayerPhobiaState
    {
        public int fearPoints;
        public int couragePoints;
        public bool hasPhobia;
        public bool isImmune;
        public bool isBurden;
        public int currentRank;
        public List<PhobiaDebuff> activeDebuffs = new List<PhobiaDebuff>();
    }
}
