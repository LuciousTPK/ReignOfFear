namespace ReignOfFear.Content.Systems.FearSystem
{
    /// <summary>
    /// This file is meant to contain, to the best of its abilities, every effect that can be
    /// given from phobia debuffs. This file has many classes meant to separate phobia debuff
    /// logic based on the type or specificity of the debuff
    /// 
    /// Ex. EnemyPhobiaEffects (phobia debuffs of the 'enemy' type), KinemortophobiaEffects (phobia debuffs
    /// specific to the phobia Kinemortophobia)
    /// 
    /// If possible, do not apply debuff logic directly into the pre-established checks and methods of other
    /// classes. If you need a reference for how they should be applied, look at how
    /// EnemyPhobiaEffects.ApplyTerrorRadius() and EnemyPhobiaEffects.ApplyTraumaticStrike() is utilized in
    /// FearSystemPlayer.cs
    /// </summary>
    public static class EnemyPhobiaEffects
    {

    }
}