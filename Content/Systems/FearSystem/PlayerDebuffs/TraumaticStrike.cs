using Terraria;
using Terraria.ModLoader;

namespace ReignOfFear.Content.Systems.FearSystem.PlayerDebuffs
{
    /// <summary>
    /// Enemy type phobia debuff that doubles all incoming fear while active
    /// </summary>
    public class TraumaticStrike : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = false;
            Main.buffNoSave[Type] = false;
        }
    }
}