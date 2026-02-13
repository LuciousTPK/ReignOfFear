using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using static System.Net.Mime.MediaTypeNames;

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

        private const int REFERENCE_HP = 100;

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

            if (info.DamageSource.SourceNPCIndex < 0 || info.DamageSource.SourceNPCIndex >= Main.maxNPCs)
                return;

            NPC sourceNPC = Main.npc[info.DamageSource.SourceNPCIndex];

            if (!sourceNPC.active)
                return;

            CombatTracker.RecordEnemyDamage(sourceNPC, Player.whoAmI, info.Damage);

            if (info.Damage > 0 && Player.statLife - info.Damage > 0)
            {
                PhobiaData.NPCPhobiaMap.TryGetValue(sourceNPC.type, out List<PhobiaID> phobias);
                if (phobias != null)
                {
                    int fearPoints = CalculateFearProgression(info.Damage, Player.statLifeMax2, Player.statLife);
                    foreach (PhobiaID phobia in phobias)
                    {
                        AddFearPoints(phobia, fearPoints);
                    }
                }
            }
        }

        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        {
            base.Kill(damage, hitDirection, pvp, damageSource);

            CombatTracker.OnPlayerDeath(Player.whoAmI);

            if (damageSource.SourceNPCIndex >= 0 && damageSource.SourceNPCIndex < Main.maxNPCs)
            {
                NPC sourceNPC = Main.npc[damageSource.SourceNPCIndex];
                if (PhobiaData.NPCPhobiaMap.TryGetValue(sourceNPC.type, out List<PhobiaID> phobias))
                {
                    foreach (PhobiaID phobia in phobias)
                    {
                        PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition definition);
                        int deathPenalty = definition.postAcquisitionMax / 3;
                        AddFearPoints(phobia, deathPenalty);
                    }
                }
            }
        }
        
        private int CalculateFearProgression(int damage, int maxHP, int currentHP)
        {
            float normalizedDamage = damage * (REFERENCE_HP / (float)maxHP);
            float healthModifier = 1.0f - ((float)currentHP / maxHP) * 0.5f;
            float fearPoints = normalizedDamage * healthModifier;

            return (int)Math.Floor(fearPoints);
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

            if (!playerPhobiaData[phobia].hasPhobia)
            {
                if (playerPhobiaData[phobia].fearPoints > 0)
                {
                    playerPhobiaData[phobia].fearPoints = Math.Max(0, playerPhobiaData[phobia].fearPoints - points);
                }

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

        public bool HasDebuff(PhobiaID phobia, PhobiaDebuff.PhobiaDebuffID debuffID)
        {
            if (!playerPhobiaData[phobia].hasPhobia) return false;
            return playerPhobiaData[phobia].activeDebuffs.Any(d => d.id == debuffID);
        }

        public bool HasDebuffAtRank(PhobiaID phobia, int rank)
        {
            if (!playerPhobiaData[phobia].hasPhobia) return false;
            return playerPhobiaData[phobia].activeDebuffs.Any(d => d.rank == rank);
        }
    }
}
