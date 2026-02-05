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
            // Main.NewText($"[SPAWN] NPC {npc.type}: whoAmI={npc.whoAmI}, realLife={npc.realLife}", Color.Yellow);
            //Main.NewText($"  ai[0]={npc.ai[0]}, ai[1]={npc.ai[1]}, ai[2]={npc.ai[2]}, ai[3]={npc.ai[3]}", Color.Cyan);

            Main.NewText($"[SPAWN] Frame {Main.GameUpdateCount}: Type={npc.type}, whoAmI={npc.whoAmI}, Pos=({(int)npc.position.X}, {(int)npc.position.Y})", Color.Cyan);
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

            if (npc.type == 127)
            {
                Main.NewText($"[HEAD DIED] Checking for remaining parts...", Color.Yellow);
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC otherNPC = Main.npc[i];
                    if (!otherNPC.active) continue;
                    if (otherNPC.type >= 128 && otherNPC.type <= 131 && otherNPC.ai[1] == npc.whoAmI)
                    {
                        Main.NewText($"  Arm {otherNPC.type}: active={otherNPC.active}, life={otherNPC.life}/{otherNPC.lifeMax}", Color.Cyan);
                    }
                }
            }
        }
    }
}
