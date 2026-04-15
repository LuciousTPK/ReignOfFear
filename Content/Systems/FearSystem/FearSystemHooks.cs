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
                    int totalPhobias = modPlayer.GetTotalPhobiaCount();
                    float extensionMultiplier = Math.Min(
                        afflictionsRank * 0.021f * totalPhobias, 0.75f);

                    time = (int)(time * (1f + extensionMultiplier));
                }
            }

            orig(self, type, time, quiet, foodHack);
        }
    }
}