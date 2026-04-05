using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ReignOfFear.Content.Systems.FearSystem
{
    public class ProjectileSourceDebug : GlobalProjectile
    {
        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            string sourceName = source?.GetType().Name ?? "null";
            string entityInfo = "none";

            if (source is EntitySource_Parent parent)
            {
                if (parent.Entity is NPC npc)
                    entityInfo = $"NPC: {npc.FullName} (type: {npc.type})";
                else if (parent.Entity is Player player)
                    entityInfo = $"Player: {player.name}";
                else
                    entityInfo = $"Entity: {parent.Entity?.GetType().Name ?? "null"}";
            }

            Main.NewText($"[Projectile {projectile.type}] {projectile.Name} | Source: {sourceName} | Entity: {entityInfo}", 200, 200, 200);
        }
    }
}