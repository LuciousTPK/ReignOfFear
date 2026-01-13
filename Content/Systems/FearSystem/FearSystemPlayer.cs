using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ReignOfFear.Content.Systems.FearSystem
{
    public enum PhobiaID
    {
        Kinemortophobia,
        Phasmophobia,
        Skelephobia
    }

    internal class FearSystemPlayer : ModPlayer
    {
        Dictionary<PhobiaID, PlayerPhobiaState> playerPhobiaData = new Dictionary<PhobiaID, PlayerPhobiaState>();

        public override void Initialize()
        {
            base.Initialize();

            foreach (PhobiaID phobia in Enum.GetValues<PhobiaID>())
            {
                playerPhobiaData[phobia] = new PlayerPhobiaState();
                //We'd probably have some lines down here that took info from a Save/Load file and
                //Applied the information to the PlayerPhobiaState instance, but since we don't have
                //That we will have to play pretend and just say that logic is here
            }
        }

        public override void OnHurt(Player.HurtInfo info)
        {
            base.OnHurt(info);

            Main.NewText("OnHurt fired! Damage: " + info.Damage, Color.Red);
            Main.NewText("SourceNPCIndex: " + info.DamageSource.SourceNPCIndex, Color.Yellow);

            if (info.Damage > 0)
            {
                NPC sourceNPC = Main.npc[info.DamageSource.SourceNPCIndex];
                PhobiaData.NPCPhobiaMap.TryGetValue(sourceNPC.type, out List<PhobiaID> phobias);

                if (phobias != null)
                {
                    foreach (PhobiaID phobia in phobias)
                    {
                        AddFearPoints(phobia, 2);
                    }
                }
            }
        }

        public void AddFearPoints(PhobiaID phobia, float points)
        {
            playerPhobiaData[phobia].fearPoints += points;
        }

        public void RemoveFearPoints(PhobiaID phobia, float points)
        {
            playerPhobiaData[phobia].fearPoints -= points ;
        }

        public PlayerPhobiaState GetPhobiaState(PhobiaID phobia)
        {
            return playerPhobiaData[phobia];
        }

        public bool HasPhobia(PhobiaID phobia)
        {
            return playerPhobiaData[phobia].hasPhobia;
        }
    }
}
