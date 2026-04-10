using System;
using System.Collections.Generic;

namespace ReignOfFear.Content.Systems.FearSystem
{
    public enum SetID
    {
        Nature,
        Underground,
        Dungeon,
        Evil,
        Jungle,
        Hell,
        Hallow,
        Constructs,
        Ocean,
        Desert,
        Snow,
        Slimes,
        Horror,
        Invasions,
        Space,
        Afflictions,
        Undead,
        Animals
    }

    /// <summary>
    /// This file is a container for both the Set data
    /// the current state of the Set
    /// and data regarding the Set's thresholds
    /// </summary>
    public class PhobiaSet
    {
        public int rank1Threshold;
        public int rank2Threshold;
        public int rank3Threshold;
    }

    internal class PlayerSetState
    {
        public int currentRank = 0;
    }

    public static class PhobiaSetData
    {
        public static Dictionary<SetID, PhobiaSet> Definitions = new Dictionary<SetID, PhobiaSet>
        {
            { SetID.Undead,       new PhobiaSet { rank1Threshold = 1, rank2Threshold = 2, rank3Threshold = 3 } },
            { SetID.Animals,      new PhobiaSet { rank1Threshold = 1, rank2Threshold = 3, rank3Threshold = 5 } },
            { SetID.Nature,       new PhobiaSet { rank1Threshold = 1, rank2Threshold = 2, rank3Threshold = 3 } },
            { SetID.Underground,  new PhobiaSet { rank1Threshold = 1, rank2Threshold = 2, rank3Threshold = 3 } },
            { SetID.Dungeon,      new PhobiaSet { rank1Threshold = 1, rank2Threshold = 2, rank3Threshold = 3 } },
            { SetID.Evil,         new PhobiaSet { rank1Threshold = 2, rank2Threshold = 4, rank3Threshold = 6 } },
            { SetID.Jungle,       new PhobiaSet { rank1Threshold = 1, rank2Threshold = 3, rank3Threshold = 5 } },
            { SetID.Hell,         new PhobiaSet { rank1Threshold = 1, rank2Threshold = 2, rank3Threshold = 3 } },
            { SetID.Hallow,       new PhobiaSet { rank1Threshold = 1, rank2Threshold = 2, rank3Threshold = 3 } },
            { SetID.Constructs,   new PhobiaSet { rank1Threshold = 1, rank2Threshold = 3, rank3Threshold = 5 } },
            { SetID.Ocean,        new PhobiaSet { rank1Threshold = 1, rank2Threshold = 2, rank3Threshold = 3 } },
            { SetID.Desert,       new PhobiaSet { rank1Threshold = 1, rank2Threshold = 2, rank3Threshold = 3 } },
            { SetID.Snow,         new PhobiaSet { rank1Threshold = 1, rank2Threshold = 2, rank3Threshold = 3 } },
            { SetID.Slimes,       new PhobiaSet { rank1Threshold = 1, rank2Threshold = 2, rank3Threshold = 3 } },
            { SetID.Horror,       new PhobiaSet { rank1Threshold = 1, rank2Threshold = 2, rank3Threshold = 3 } },
            { SetID.Invasions,    new PhobiaSet { rank1Threshold = 1, rank2Threshold = 2, rank3Threshold = 3 } },
            { SetID.Space,        new PhobiaSet { rank1Threshold = 1, rank2Threshold = 2, rank3Threshold = 3 } },
            { SetID.Afflictions,  new PhobiaSet { rank1Threshold = 1, rank2Threshold = 3, rank3Threshold = 5 } },
        };
    }
}