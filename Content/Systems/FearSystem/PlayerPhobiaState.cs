namespace ReignOfFear.Content.Systems.FearSystem
{
    internal class PlayerPhobiaState
    {
        public float fearPoints;
        public bool hasPhobia;
        public bool isImmune;
        public bool isBurden;
        public int currentRank;
        public List<PhobiaDebuff> activeDebuffs = new List<PhobiaDebuff>();
    }
}
