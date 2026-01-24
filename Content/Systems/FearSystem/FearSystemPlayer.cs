using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public PlayerPhobiaState GetPhobiaState(PhobiaID phobia)
        {
            return playerPhobiaData[phobia];
        }

        public bool HasPhobia(PhobiaID phobia)
        {
            return playerPhobiaData[phobia].hasPhobia;
        }

        public void AddFearPoints(PhobiaID phobia, int points)
        {
            PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition definition);

            if (playerPhobiaData[phobia].isBurden)
            {
                return;
            }

            if (playerPhobiaData[phobia].hasPhobia)
            {
                if (playerPhobiaData[phobia].couragePoints == 0)
                {
                    playerPhobiaData[phobia].fearPoints += points;

                    if (playerPhobiaData[phobia].fearPoints > definition.postAcquisitionMax)
                    {
                        playerPhobiaData[phobia].fearPoints = definition.postAcquisitionMax;
                    }
                }

                else
                {
                    if (playerPhobiaData[phobia].couragePoints - points >= 0)
                    {
                        playerPhobiaData[phobia].couragePoints -= points;
                    }

                    else
                    {
                        points = Math.Abs(playerPhobiaData[phobia].couragePoints - points);
                        playerPhobiaData[phobia].couragePoints = 0;
                        playerPhobiaData[phobia].fearPoints += points;

                        if (playerPhobiaData[phobia].fearPoints > definition.postAcquisitionMax)
                        {
                            playerPhobiaData[phobia].fearPoints = definition.postAcquisitionMax;
                        }
                    }
                }

                int calculatedRank = definition.CalculateRank(playerPhobiaData[phobia].fearPoints, playerPhobiaData[phobia].currentRank, true);
                HandlePhobiaRank(phobia, calculatedRank);
            }

            else
            {
                playerPhobiaData[phobia].fearPoints += points;

                if (playerPhobiaData[phobia].fearPoints >= definition.preAcquisitionMax)
                {
                    playerPhobiaData[phobia].fearPoints = playerPhobiaData[phobia].fearPoints - definition.preAcquisitionMax;
                    playerPhobiaData[phobia].hasPhobia = true;

                    if (playerPhobiaData[phobia].fearPoints > definition.postAcquisitionMax)
                    {
                        playerPhobiaData[phobia].fearPoints = definition.postAcquisitionMax;
                    }

                    int calculatedRank = definition.CalculateRank(playerPhobiaData[phobia].fearPoints, playerPhobiaData[phobia].currentRank, true);
                    HandlePhobiaRank(phobia, calculatedRank);
                }
            }
        }

        public void RemoveFearPoints(PhobiaID phobia, int points)
        {
            playerPhobiaData[phobia].fearPoints -= points;
        }

        public void SetFearPoints(PhobiaID phobia, int points)
        {
            if (playerPhobiaData[phobia].isBurden)
            {
                return;
            }

            PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition definition);

            playerPhobiaData[phobia].fearPoints = points;
            playerPhobiaData[phobia].couragePoints = 0;

            if (playerPhobiaData[phobia].hasPhobia)
            {
                if (playerPhobiaData[phobia].fearPoints > definition.postAcquisitionMax)
                {
                    playerPhobiaData[phobia].fearPoints = definition.postAcquisitionMax;
                }

                bool increasing = points > playerPhobiaData[phobia].fearPoints;
                int calculatedRank = definition.CalculateRank(playerPhobiaData[phobia].fearPoints, playerPhobiaData[phobia].currentRank, increasing);
                HandlePhobiaRank(phobia, calculatedRank);
            }
            else
            {
                if (playerPhobiaData[phobia].fearPoints > definition.preAcquisitionMax)
                {
                    playerPhobiaData[phobia].fearPoints = definition.preAcquisitionMax;
                }
            }
        }

        public void AddCouragePoints(PhobiaID phobia, int points)
        {
            if (playerPhobiaData[phobia].isBurden)
            {
                return;
            }

            PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition definition);

            if (playerPhobiaData[phobia].fearPoints == 0)
            {
                playerPhobiaData[phobia].couragePoints += points;

                if (playerPhobiaData[phobia].couragePoints > definition.courageMax)
                {
                    playerPhobiaData[phobia].couragePoints = definition.courageMax;
                }
            }

            else
            {
                if (playerPhobiaData[phobia].fearPoints - points >= 0)
                {
                    playerPhobiaData[phobia].fearPoints -= points;
                }

                else
                {
                    points = Math.Abs(playerPhobiaData[phobia].fearPoints - points);
                    playerPhobiaData[phobia].fearPoints = 0;
                    playerPhobiaData[phobia].couragePoints += points;

                    if (playerPhobiaData[phobia].couragePoints > definition.courageMax)
                    {
                        playerPhobiaData[phobia].couragePoints = definition.courageMax;
                    }
                }
            }

            int calculatedRank = definition.CalculateRank(playerPhobiaData[phobia].fearPoints, playerPhobiaData[phobia].currentRank, false);
            HandlePhobiaRank(phobia, calculatedRank);
        }

        public void RemoveCouragePoints(PhobiaID phobia, int points)
        {
            playerPhobiaData[phobia].couragePoints -= points;
        }

        public void SetCouragePoints(PhobiaID phobia, int points)
        {
            if (playerPhobiaData[phobia].isBurden)
            {
                return;
            }

            PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition definition);

            playerPhobiaData[phobia].couragePoints = points;
            playerPhobiaData[phobia].fearPoints = 0;

            if (playerPhobiaData[phobia].couragePoints > definition.courageMax)
            {
                playerPhobiaData[phobia].couragePoints = definition.courageMax;
            }

            int calculatedRank = definition.CalculateRank(playerPhobiaData[phobia].fearPoints, playerPhobiaData[phobia].currentRank, false);
            HandlePhobiaRank(phobia, calculatedRank);
        }

        public void HandlePhobiaRank(PhobiaID phobia, int calculatedRank)
        {
            if (playerPhobiaData[phobia].currentRank == calculatedRank)
            {
                return;
            }

            if (calculatedRank > playerPhobiaData[phobia].currentRank)
            {
                while (playerPhobiaData[phobia].currentRank != calculatedRank)
                {
                    playerPhobiaData[phobia].currentRank++;

                    if (playerPhobiaData[phobia].currentRank == 4)
                    {
                        playerPhobiaData[phobia].isBurden = true;
                        AddPhobiaDebuff(phobia, playerPhobiaData[phobia].currentRank);
                        break;
                    }

                    AddPhobiaDebuff(phobia, playerPhobiaData[phobia].currentRank);
                }
            }
            else
            {
                while (playerPhobiaData[phobia].currentRank != calculatedRank)
                {
                    RemovePhobiaDebuff(phobia, playerPhobiaData[phobia].currentRank);
                    playerPhobiaData[phobia].currentRank--;
                }
            }
        }

        private void AddPhobiaDebuff(PhobiaID phobia, int rank)
        {
            PhobiaDebuff debuff = PhobiaDebuffData.SelectDebuff(phobia, rank);
            playerPhobiaData[phobia].activeDebuffs.Add(debuff);
        }

        private void RemovePhobiaDebuff(PhobiaID phobia, int rank)
        {
            PhobiaDebuff debuff = playerPhobiaData[phobia].activeDebuffs.FirstOrDefault(activeDebuff => activeDebuff.rank == rank);
            playerPhobiaData[phobia].activeDebuffs.Remove(debuff);
        }
    }
}
