using Microsoft.Xna.Framework;
using ReignOfFear.Content.Systems.FearSystem.PlayerDebuffs;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace ReignOfFear.Content.Systems.FearSystem
{
    public static class EnemyPhobiaEffects
    {
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