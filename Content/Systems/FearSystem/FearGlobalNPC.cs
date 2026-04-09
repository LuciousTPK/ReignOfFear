using Microsoft.Xna.Framework;
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
