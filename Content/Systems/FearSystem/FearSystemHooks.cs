using Humanizer;
using System;
using Terraria;
using Terraria.ModLoader;

namespace ReignOfFear.Content.Systems.FearSystem
{
    /// <summary>
    /// Contains MonoMod detours for vanilla Terraria methods that the Fear System
    /// needs to intercept but cannot through standard tModLoader hooks.
    /// </summary>

    public class FearSystemHooks : ModSystem
    {
        public override void Load()
        {
            Terraria.On_Player.AddBuff += OnPlayerAddBuff;
        }

        public override void Unload()
        {
            Terraria.On_Player.AddBuff -= OnPlayerAddBuff;
        }

        private static void OnPlayerAddBuff(
            Terraria.On_Player.orig_AddBuff orig,
            Player self,
            int type,
            int time,
            bool quiet,
            bool foodHack)
        {
            if (PhobiaData.DebuffPhobiaMap.ContainsKey(type))
            {
                FearSystemPlayer modPlayer = self.GetModPlayer<FearSystemPlayer>();

                int afflictionsRank = modPlayer.GetSetRank(SetID.Afflictions);
                if (afflictionsRank > 0)
                {
                    float diffMult = 1f;
                    if (Main.masterMode) diffMult = 2.67f;
                    else if (Main.expertMode) diffMult = 1.45f;

                    int totalPhobias = modPlayer.GetTotalPhobiaCount();
                    float extensionMultiplier = Math.Min(
                        afflictionsRank * 0.015f * diffMult * totalPhobias, 1.5f);

                    time = (int)(time * (1f + extensionMultiplier));
                }
            }

            orig(self, type, time, quiet, foodHack);
        }
    }
}