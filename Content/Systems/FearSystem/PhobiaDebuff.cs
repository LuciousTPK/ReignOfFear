using System.Collections.Generic;

namespace ReignOfFear.Content.Systems.FearSystem
{
    /// <summary>
    /// This is a container class that contains data on phobia debuffs, allowing us to assign
    /// a type, ID, and what rank it is associated with. For new debuffs to be made, their ID
    /// needs to be added to the PhobiaDebuffID with comments to separate them by type/phobia
    /// </summary>

    public class PhobiaDebuff
    {
        public enum PhobiaDebuffID
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

        public PhobiaDebuffID id;
        public PhobiaDefinition.PhobiaType type;
        public int rank;
    }

    /// <summary>
    /// This class is used to actually initialize the phobia debuff instances with whatever data that is associated with them
    /// (I.E. debuff ID, type, and associated rank). If you want to add new phobia debuffs, this is the place to add them,
    /// keeping in mind that type debuffs are religated to the typeDebuffs dictionary and phobia specific debuffs to the
    /// phobiaSpecificDebuffs dictionary
    /// </summary>

    public static class PhobiaDebuffData
    {
        public static Dictionary<PhobiaDefinition.PhobiaType, List<PhobiaDebuff>> typeDebuffs = new Dictionary<PhobiaDefinition.PhobiaType, List<PhobiaDebuff>>
        {
            {
                PhobiaDefinition.PhobiaType.Enemy,
                new List<PhobiaDebuff>
                {

                }
            }
        };

        public static Dictionary<PhobiaID, List<PhobiaDebuff>> phobiaSpecificDebuffs = new Dictionary<PhobiaID, List<PhobiaDebuff>>
        {
            {
                PhobiaID.Kinemortophobia,
                new List<PhobiaDebuff>
                {
                    
                }
            }
        };
    }
}
