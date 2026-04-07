using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ReignOfFear.Content.Systems.FearSystem
{
    /// <summary>
    /// Tracks the original NPC source of every hostile projectile in the game
    /// 
    /// We cache projectiles at spawn time so the origin is always available at
    /// hit time regardless of whether parent projectiles are still alive
    ///
    /// sourceNPCIndex values:
    ///   >= 0  : specific NPC whoAmI
    ///   -1    : general damage (natural spawn, tile, unknown)
    ///   -2    : player-owned projectile — ignore entirely
    /// </summary>

    public class FearGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public int sourceNPCIndex = -1;

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            sourceNPCIndex = ResolveSource(projectile, source);
        }

        private static int ResolveSource(Projectile projectile, IEntitySource source)
        {
            if (source is EntitySource_Parent { Entity: NPC npc })
                return npc.whoAmI;

            if (source is EntitySource_Parent { Entity: Player })
                return -2;

            if (source is EntitySource_Parent { Entity: Projectile parentProj })
            {
                if (parentProj.active)
                {
                    var parentData = parentProj.GetGlobalProjectile<FearGlobalProjectile>();
                    return parentData.sourceNPCIndex;
                }
                return -1;
            }
            return -1;
        }
    }
}