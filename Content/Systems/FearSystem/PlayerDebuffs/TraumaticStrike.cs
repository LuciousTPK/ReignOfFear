using Terraria;
using Terraria.ModLoader;

namespace ReignOfFear.Content.Systems.FearSystem.PlayerDebuffs
{
    public class TraumaticStrike : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;  // This is a debuff (red icon border)
            Main.pvpBuff[Type] = false; // Can't be inflicted in PvP
            Main.buffNoSave[Type] = true; // Won't save when you exit the game
        }

        //public override void Update(Player player, ref int buffIndex)
        //{
            // The debuff effect is already handled in ApplyTraumaticStrike
            // This just needs to exist so the buff shows in the player's buff bar
        //}
    }
}