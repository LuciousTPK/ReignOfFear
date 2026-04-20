using System.Collections.Generic;

namespace ReignOfFear.Content.Systems.FearSystem
{
    /// <summary>
    /// This is a container class that contains data on phobia effects, allowing us to assign
    /// a type, ID, and what rank it is associated with. For new effectss to be made, their ID
    /// needs to be added to the PhobiaEffectID with comments to separate them by type/phobia
    /// </summary>

    public class PhobiaEffectData
    {
        public enum PhobiaEffectID
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

        public PhobiaEffectID id;
        public PhobiaDefinition.PhobiaType type;
        public int rank;
    }

    /// <summary>
    /// This class is used to actually initialize the phobia effect instances with whatever data that is associated with them
    /// (I.E. effect ID, type, and associated rank). If you want to add new phobia effects, this is the place to add them,
    /// keeping in mind that type effects are religated to the typeEffects dictionary and phobia specific effects to the
    /// phobiaSpecificEffects dictionary
    /// </summary>

    public static class PhobiaEffectMap
    {
        public static Dictionary<PhobiaDefinition.PhobiaType, List<PhobiaEffectData>> typeEffects = new Dictionary<PhobiaDefinition.PhobiaType, List<PhobiaEffectData>>
        {
            {
                PhobiaDefinition.PhobiaType.Enemy,
                new List<PhobiaEffectData>
                {

                }
            }
        };

        public static Dictionary<PhobiaID, List<PhobiaEffectData>> phobiaSpecificEffects = new Dictionary<PhobiaID, List<PhobiaEffectData>>
        {
            {
                PhobiaID.Kinemortophobia,
                new List<PhobiaEffectData>
                {
                    
                }
            }
        };
    }
}
