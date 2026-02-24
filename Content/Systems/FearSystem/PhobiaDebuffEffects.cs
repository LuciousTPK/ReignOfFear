using Microsoft.Xna.Framework;
using ReignOfFear.Content.Systems.FearSystem.PlayerDebuffs;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

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
        // Applies a 50% boost to all incoming Fear if the player is within 25 tiles or less of an enemy related to a phobia with this active debuff
        public static float ApplyTerrorRadius(Player player, PhobiaID phobia)
        {
            FearSystemPlayer modPlayer = player.GetModPlayer<FearSystemPlayer>();

            if (!modPlayer.HasDebuff(phobia, PhobiaDebuff.PhobiaDebuffID.TerrorRadius))
                return 0f;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active) continue;

                if (!PhobiaData.NPCPhobiaMap.TryGetValue(npc.type, out List<PhobiaID> npcPhobias))
                    continue;
                if (!npcPhobias.Contains(phobia))
                    continue;

                float distance = Vector2.Distance(player.Center, npc.Center);
                if (distance <= 25 * 16)
                {
                    return 0.5f;
                }
            }

            return 0f;
        }

        // Applies a 100% boost to all incoming Fear if the player is under the effect "Traumatic Strike"
        public static float ApplyTraumaticStrike(Player player, PhobiaID phobia)
        {
            FearSystemPlayer modPlayer = player.GetModPlayer<FearSystemPlayer>();

            if (!modPlayer.HasDebuff(phobia, PhobiaDebuff.PhobiaDebuffID.TraumaticStrike))
                return 0f;

            if (player.HasBuff(ModContent.BuffType<TraumaticStrike>()))
            {
                return 1.0f;
            }

            return 0f;
        }
    }
}