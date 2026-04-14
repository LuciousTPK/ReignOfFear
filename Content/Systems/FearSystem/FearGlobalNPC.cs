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
            {
                Main.NewText($"[SetDebug] {npc.FullName} has no phobia mapping — skipping", Color.Gray);
                return;
            }

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

            Main.NewText($"[SetDebug] {npc.FullName} spawned for {player.name} | Candidate sets: {string.Join(", ", candidateSets)}", Color.Yellow);

            float diffMult = 1f;
            if (Main.masterMode) diffMult = 2.67f;
            else if (Main.expertMode) diffMult = 1.45f;

            int totalPhobias = modPlayer.GetTotalPhobiaCount();

            int bestRank = 0;
            foreach (SetID setID in candidateSets)
            {
                int rank = modPlayer.GetSetRank(setID);
                Main.NewText($"[SetDebug]   {setID} rank = {rank}", Color.Yellow);
                if (rank > bestRank)
                    bestRank = rank;
            }

            Main.NewText($"[SetDebug] Best rank: {bestRank} | Total phobias: {totalPhobias} | diffMult: {diffMult}", Color.Yellow);

            if (bestRank <= 0)
            {
                Main.NewText($"[SetDebug] No active set ranks — no bonus applied", Color.Gray);
                return;
            }

            bool isBoss = npc.boss || NPCID.Sets.BossHeadTextures[npc.type] >= 0;
            int baseLife = npc.lifeMax;
            int baseDamage = npc.damage;

            if (isBoss)
            {
                float hpBonus = Math.Min(bestRank * 0.016f * diffMult * totalPhobias, 1.5f);
                float damageBonus = Math.Min(bestRank * 0.031f * diffMult * totalPhobias, 3.0f);

                npc.lifeMax = (int)(npc.lifeMax * (1f + hpBonus));
                npc.life = npc.lifeMax;
                npc.damage = (int)(npc.damage * (1f + damageBonus));

                Main.NewText($"[SetDebug] BOSS {npc.FullName} | HP: {baseLife} → {npc.lifeMax} (+{hpBonus:P0}) | Dmg: {baseDamage} → {npc.damage} (+{damageBonus:P0})", Color.Red);
            }
            else
            {
                float hpBonus = Math.Min(bestRank * 0.031f * diffMult * totalPhobias, 3.0f);
                float damageBonus = Math.Min(bestRank * 0.031f * diffMult * totalPhobias, 3.0f);
                float defenseBonus = Math.Min(bestRank * 0.031f * diffMult * totalPhobias, 3.0f);
                float kbBonus = Math.Min(bestRank * 0.010f * diffMult * totalPhobias, 1.0f);

                int baseDefense = npc.defense;
                float baseKB = npc.knockBackResist;

                npc.lifeMax = (int)(npc.lifeMax * (1f + hpBonus));
                npc.life = npc.lifeMax;
                npc.damage = (int)(npc.damage * (1f + damageBonus));
                npc.defense = (int)(npc.defense * (1f + defenseBonus));
                npc.knockBackResist = Math.Min(1f, npc.knockBackResist + kbBonus);

                Main.NewText($"[SetDebug] {npc.FullName} | HP: {baseLife} → {npc.lifeMax} | Dmg: {baseDamage} → {npc.damage} | Def: {baseDefense} → {npc.defense} | KB: {baseKB:F2} → {npc.knockBackResist:F2}", Color.Green);
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
