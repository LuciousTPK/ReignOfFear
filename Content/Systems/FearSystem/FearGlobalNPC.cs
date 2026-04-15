using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ReignOfFear.Content.Systems.FearSystem
{
    /// <summary>
    /// This file is used as our primary tracker for NPC instances. Generally speaking, most logic regarding
    /// courage accumulation is done here since killing enemies is the primary way to gain it. Think of this file
    /// as the opposite of FearSystemPlayer which has many trackers for Fear progression. We also use this file
    /// to track when combat instances are created for enemies based on player interaction rather than enemy
    /// interaction
    /// </summary>

    public class FearGlobalNPC : GlobalNPC
    {
        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            base.OnSpawn(npc, source);
            ApplySetPassiveBonus(npc);
        }

        private static void ApplySetPassiveBonus(NPC npc)
        {
            if (!PhobiaData.NPCPhobiaMap.TryGetValue(npc.type, out List<PhobiaID> phobias))
                return;

            Player player = null;

            if (npc.target >= 0 && npc.target < Main.maxPlayers && Main.player[npc.target].active)
            {
                player = Main.player[npc.target];
            }
            else
            {
                float closestDist = float.MaxValue;
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player candidate = Main.player[i];
                    if (candidate == null || !candidate.active) continue;

                    float dist = Vector2.Distance(npc.Center, candidate.Center);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        player = candidate;
                    }
                }
            }

            if (player == null)
                return;

            FearSystemPlayer modPlayer = player.GetModPlayer<FearSystemPlayer>();

            var candidateSets = new HashSet<SetID>();
            foreach (PhobiaID phobia in phobias)
            {
                if (PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition def))
                    candidateSets.Add(def.set);
            }

            int totalPhobias = modPlayer.GetTotalPhobiaCount();

            int bestRank = 0;
            foreach (SetID setID in candidateSets)
            {
                int rank = modPlayer.GetSetRank(setID);
                if (rank > bestRank)
                    bestRank = rank;
            }

            if (bestRank <= 0)
                return;

            bool isBoss = npc.boss || NPCID.Sets.BossHeadTextures[npc.type] >= 0;

            if (isBoss)
            {
                float hpBonus = Math.Min(bestRank * 0.014f * totalPhobias, 0.5f);
                float damageBonus = Math.Min(bestRank * 0.028f * totalPhobias, 1.0f);
                float defenseBonus = Math.Min(bestRank * 0.014f * totalPhobias, 0.25f);

                npc.lifeMax = (int)(npc.lifeMax * (1f + hpBonus));
                npc.life = npc.lifeMax;
                npc.damage = (int)(npc.damage * (1f + damageBonus));
                npc.defense = (int)(npc.defense * (1f + defenseBonus));
            }
            else
            {
                float hpBonus = Math.Min(bestRank * 0.014f * totalPhobias, 0.5f);
                float damageBonus = Math.Min(bestRank * 0.028f * totalPhobias, 1.0f);
                float defenseBonus = Math.Min(bestRank * 0.007f * totalPhobias, 0.25f);
                float kbBonus = bestRank * 0.04f;

                npc.lifeMax = (int)(npc.lifeMax * (1f + hpBonus));
                npc.life = npc.lifeMax;
                npc.damage = (int)(npc.damage * (1f + damageBonus));
                npc.defense = (int)(npc.defense * (1f + defenseBonus));
                npc.knockBackResist = Math.Min(1f, npc.knockBackResist + kbBonus);
            }
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            CombatTracker.RecordPlayerDamage(npc, player.whoAmI, 0);
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (projectile.owner >= 0 && projectile.owner < Main.maxPlayers)
            {
                Player player = Main.player[projectile.owner];
                if (player != null && player.active)
                {
                    CombatTracker.RecordPlayerDamage(npc, player.whoAmI, 0);
                }
            }
        }

        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            CombatTracker.RecordPlayerDamage(npc, player.whoAmI, damageDone);
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (projectile.owner >= 0 && projectile.owner < Main.maxPlayers)
            {
                Player player = Main.player[projectile.owner];
                if (player != null && player.active)
                {
                    CombatTracker.RecordPlayerDamage(npc, player.whoAmI, damageDone);
                }
            }
        }

        public override void OnKill(NPC npc)
        {
            CombatTracker.OnEnemyKilled(npc);
        }
    }
}
