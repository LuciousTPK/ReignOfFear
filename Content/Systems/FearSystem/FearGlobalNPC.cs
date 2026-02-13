using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ReignOfFear.Content.Systems.FearSystem
{
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
