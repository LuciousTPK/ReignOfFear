using System.Collections.Generic;

namespace ReignOfFear.Content.Systems.FearSystem
{
    /// <summary>
    /// Similar to how PhobiaDefinition creates an instance of static phobia data
    /// this class also creates an instance of phobia data, however this data
    /// is the data that gets tracked, changed, or is otherwise interacted with
    /// by the player/mod
    /// 
    /// It includes the current fear/courage value, if the player has it,
    /// if they are immune to it, if its a burden, active effects, and
    /// the current rank of the phobia
    /// </summary>

    internal class PlayerPhobiaState
    {
        public int fearPoints;
        public int couragePoints;
        public bool hasPhobia;
        public bool isImmune;
        public bool isBurden;
        public int currentRank = 1;
        public List<PhobiaEffectData> activeEffects = new List<PhobiaEffectData>();
    }
}
