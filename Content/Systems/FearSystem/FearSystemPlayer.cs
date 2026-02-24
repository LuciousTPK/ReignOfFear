using ReignOfFear.Content.Systems.FearSystem.PlayerDebuffs;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ReignOfFear.Content.Systems.FearSystem
{
    // Defines phobias based on their name--acts as a key for PhobiaData and PlayerPhobiaStates
    public enum PhobiaID
    {
        Kinemortophobia,
        Phasmophobia,
        Skelephobia
    }

    /// <summary>
    /// FearSystemPlayer is the real meat of the Fear System
    /// It coordinates pretty much everything regarding fear progression
    /// and the effects that has on the phobias the player has
    /// 
    /// It is responsible for initializing phobias for each player client side
    /// saving and loading data for those phobias
    /// detecting when players should receive fear
    /// adding said fear and courage if the logic in FearGlobalNPC goes off
    /// and some helper methods regarding phobia ranks/debuffs
    /// </summary>

    internal class FearSystemPlayer : ModPlayer
    {
        Dictionary<PhobiaID, PlayerPhobiaState> playerPhobiaData = new Dictionary<PhobiaID, PlayerPhobiaState>();

        /// <remarks>
        /// Used to normalize fear progression through normal damage values
        /// The smaller the number the smaller the fear gained
        /// The bigger the number the larger the fear gained
        /// </remarks>

        private const int REFERENCE_HP = 100;

        /// <remarks>
        /// Additive multiplier used at final fear calculation
        /// 
        /// Be aware that the bonus multiplier multiplies the total fear received
        /// on any given frame, if you want a bonus to effect a portion of incoming fear
        /// it must be applied to the value before it is added to the total
        /// 
        /// Ex. "This effect boosts fear by 50% from skeletons"
        /// Ex. Fear must be boosted by 50% before being added to the total value
        /// </remarks>

        public float bonusMultiplier = 0f;

        // Initializes PlayerPhobiaStates for each phobia, then loads the data for those instances
        public override void Initialize()
        {
            base.Initialize();

            foreach (PhobiaID phobia in Enum.GetValues<PhobiaID>())
            {
                playerPhobiaData[phobia] = new PlayerPhobiaState();
                
                /// <remarks>
                /// We'd probably have some line here that would load PlayerPhobiaState
                /// data from some data base meant to contain saved phobia data, however
                /// such a saving method has yet to be created so we are just going to have
                /// to play pretend for the time being
                /// </remarks>
            }
        }

        // The primary method we use to identify what NPC/projectile hurt the player, initialize combat instances for NPCs, and add fear points based on damage
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
                        if (HasDebuff(phobia, PhobiaDebuff.PhobiaDebuffID.TraumaticStrike))
                        {
                            if (Main.rand.NextFloat() < 0.2f)
                            {
                                Player.AddBuff(ModContent.BuffType<TraumaticStrike>(), 30 * 60);
                            }
                            break;
                        }
                    }

                    foreach (PhobiaID phobia in phobias)
                    {
                        AddFearPoints(phobia, fearPoints);
                    }

                    bonusMultiplier = 0f;
                }
            }
        }

        // Similar concept to the 'OnHurt' method, but we clear combat data and apply a flat 33% fear progression for applicable phobias
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

                    bonusMultiplier = 0f;
                }
            }
        }
        
        // Helper method that produces fear points from health damage
        private int CalculateFearProgression(int damage, int maxHP, int currentHP)
        {
            float normalizedDamage = damage * (REFERENCE_HP / (float)maxHP);
            float healthModifier = 1.0f - ((float)currentHP / maxHP) * 0.5f;
            float fearPoints = normalizedDamage * healthModifier;

            return (int)Math.Floor(fearPoints);
        }

        // Helper method that returns a phobia's player state
        public PlayerPhobiaState GetPhobiaState(PhobiaID phobia)
        {
            return playerPhobiaData[phobia];
        }

        // Helper method that returns if a player has a phobia
        public bool HasPhobia(PhobiaID phobia)
        {
            return playerPhobiaData[phobia].hasPhobia;
        }

        // Contains all logic surrounding adding fear points to any phobia in the mod
        public void AddFearPoints(PhobiaID phobia, int points)
        {
            PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition definition);

            if (playerPhobiaData[phobia].isBurden)
            {
                return;
            }

            bonusMultiplier += EnemyPhobiaEffects.ApplyTerrorRadius(Player, phobia);
            bonusMultiplier += EnemyPhobiaEffects.ApplyTraumaticStrike(Player, phobia);

            points = (int)(points * (1 + bonusMultiplier));

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

                int calculatedRank = CalculateRank(definition, playerPhobiaData[phobia].fearPoints, playerPhobiaData[phobia].currentRank, true);
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

                    int calculatedRank = CalculateRank(definition, playerPhobiaData[phobia].fearPoints, playerPhobiaData[phobia].currentRank, true);
                    HandlePhobiaRank(phobia, calculatedRank);
                }
            }
        }

        // Helper method that removes fear from phobias
        public void RemoveFearPoints(PhobiaID phobia, int points)
        {
            playerPhobiaData[phobia].fearPoints -= points;
        }

        // Helper method that sets fear to some arbitrary value
        public void SetFearPoints(PhobiaID phobia, int points)
        {
            if (playerPhobiaData[phobia].isBurden)
            {
                return;
            }

            PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition definition);

            bool increasing = points > playerPhobiaData[phobia].fearPoints;

            playerPhobiaData[phobia].fearPoints = points;
            playerPhobiaData[phobia].couragePoints = 0;

            if (playerPhobiaData[phobia].hasPhobia)
            {
                if (playerPhobiaData[phobia].fearPoints > definition.postAcquisitionMax)
                {
                    playerPhobiaData[phobia].fearPoints = definition.postAcquisitionMax;
                }

                int calculatedRank = CalculateRank(definition, playerPhobiaData[phobia].fearPoints, playerPhobiaData[phobia].currentRank, increasing);
                HandlePhobiaRank(phobia, calculatedRank);
            }
            else
            {
                if (playerPhobiaData[phobia].fearPoints > definition.preAcquisitionMax)
                {
                    playerPhobiaData[phobia].fearPoints = playerPhobiaData[phobia].fearPoints - definition.preAcquisitionMax;
                    playerPhobiaData[phobia].hasPhobia = true;

                    if (playerPhobiaData[phobia].fearPoints > definition.postAcquisitionMax)
                    {
                        playerPhobiaData[phobia].fearPoints = definition.postAcquisitionMax;
                    }

                    int calculatedRank = CalculateRank(definition, playerPhobiaData[phobia].fearPoints, playerPhobiaData[phobia].currentRank, increasing);
                    HandlePhobiaRank(phobia, calculatedRank);
                }
            }
        }

        // Contains all logic surrounding adding courage points to any phobia in the mod
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

            int calculatedRank = CalculateRank(definition, playerPhobiaData[phobia].fearPoints, playerPhobiaData[phobia].currentRank, false);
            HandlePhobiaRank(phobia, calculatedRank);
        }

        // Helper method for removing courage points
        public void RemoveCouragePoints(PhobiaID phobia, int points)
        {
            if (playerPhobiaData[phobia].isBurden || !playerPhobiaData[phobia].hasPhobia)
            {
                return;
            }

            playerPhobiaData[phobia].couragePoints -= points;
        }

        // Helper method for setting courage to some arbitrary number
        public void SetCouragePoints(PhobiaID phobia, int points)
        {
            if (playerPhobiaData[phobia].isBurden || !playerPhobiaData[phobia].hasPhobia)
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

            int calculatedRank = CalculateRank(definition, playerPhobiaData[phobia].fearPoints, playerPhobiaData[phobia].currentRank, false);
            HandlePhobiaRank(phobia, calculatedRank);
        }

        /// <summary>
        /// This method is used to determine the target rank of a phobia when increasing/decreasing fear points
        /// 
        /// It uses hysteresis to prevent flickering when at the edge of a threshold,
        /// and requires effort to be put in to reduce a phobia's rank rather
        /// 
        /// Example: Rank 2 is gained at 100 points, but only removed at 0 points
        /// 
        /// Obviously we need the current rank of the phobia in question and its current fear points as points of reference
        /// Then we flag the process as either increasing or not using the 'rankIncrease' bool,
        /// which is automatically set in the AddFearPoints/SetFearPoints/AddCouragePoints/SetCouragePoints methods
        /// </summary>

        private int CalculateRank(PhobiaDefinition definition, int fearPoints, int currentRank, bool rankIncrease)
        {
            if (rankIncrease)
            {
                /// <remarks>
                /// These are absolute rules in the 'increasing' logic
                /// Obviously if fear is at max then this phobia becomes a burden
                /// Since hysteresis has no effect on numbers passed rank 3's threshold, there is no overlap,
                /// we can safetly assume any number beyond it will return as rank 3
                /// Finally, while there are overlaps between rank 1 and 2, and rank 2 and 3, the exact threshold
                /// of rank 2 itself does not have an overlap
                /// </remarks>

                if (fearPoints >= definition.postAcquisitionMax) return 4; // We use 4 for burdens since there is obviously no numeric in the name
                if (fearPoints >= definition.rank3Threshold) return 3;
                if (fearPoints == definition.rank2Threshold) return 2;

                /// <remarks>
                /// This is the first overlap, specifically the one between rank 2 and 3
                /// Since Fear is increasing, and we already handled the rank 3 check above,
                /// we know for a fact that the rank remains the same unless it is rank 1
                /// In that case the player must have crossed the rank 2 threshold
                /// </remarks>

                if (fearPoints < definition.rank3Threshold && fearPoints > definition.rank2Threshold)
                {
                    switch (currentRank)
                    {
                        case 1:
                            {
                                return 2;
                            }

                        default:
                            {
                                return currentRank;
                            }
                    }
                }

                /// <remarks>
                /// The second overlap checks for the gap in rank 1 and rank 2
                /// Generally speaking we can safely assume at this point that Fear resides in this gap
                /// since we have yet to return from the above logic, which is to say that no rank change can happen
                /// Adding the check for rank 3 and defaulting to rank 2 if that is true is mostly redundant protection,
                /// due to SetFearPoints having a bool check for if the new Fear total is less than it was before the change
                /// and SetCouragePoints auto resets a phobia to rank 1 anyways unless it's a burden
                /// 
                /// This check was mostly designed so there is a default return to make .NET happy and in some rare case where Fear
                /// gets set outside of SetFearPoints and lands Fear in this gap while the phobia is rank 3
                /// in which case the rank would be changed to rank 2
                /// </remarks>

                if (fearPoints < definition.rank2Threshold && currentRank != 3)
                {
                    return currentRank;
                }

                return 2;
            }

            else
            {
                /// <remarks>
                /// These are the absolute rules for when Fear is decreasing
                /// 
                /// Since there is no rank specific to the minimum outside of the default of rank 1
                /// there are only two absolute rules as opposed to three when increasing,
                /// that being when Fear is 0 then the phobia is always rank 1 and of course
                /// the no overlap over the rank 2 threshold still applies even here
                /// </remarks>

                if (fearPoints == 0) return 1;
                if (fearPoints == definition.rank2Threshold) return 2;

                /// <remarks>
                /// First we start with the rank 1/rank 2 overlap first, as it works identically to how the
                /// rank 2/rank 3 overlap works when fear is increasing
                /// Basically, the rank can be either 1 or 2 in this overlap, so we just return whatever the rank was
                /// and if the rank was 3 then that means the rank 2 threshold was crossed and thus the rank decreased
                /// </remarks>

                if (fearPoints > 0 && fearPoints < definition.rank2Threshold)
                {
                    switch (currentRank)
                    {
                        case 3:
                            {
                                return 2;
                            }

                        default:
                            {
                                return currentRank;
                            }
                    }
                }

                /// <remarks>
                /// This is the final overlap logic in this method
                /// The decreasing Fear version of the rank 2/rank 3 overlap
                /// 
                /// Since rank 3 encompasses both the overlap between it and rank 2
                /// and the gap between itself and the max fear, we can actually safetly
                /// return rank 3 if this check fails because that section of the gauge
                /// belongs entirely to rank 3
                /// 
                /// For this reason, we don't actually need to add a redundant check
                /// even though one was added for safety, that being the case for if
                /// current rank is 1, which shouldn't be possible while decreasing
                /// in this overlap much like rank 3 isn't possible when increasing
                /// in the rank 1/rank 2 overlap--it's just in case some future logic
                /// bypasses the SetFearPoints/SetCouragePoints methods
                /// </remarks>

                if (fearPoints > definition.rank2Threshold && fearPoints < definition.rank3Threshold)
                {
                    switch (currentRank)
                    {
                        case 1:
                            {
                                return 2;
                            }

                        default:
                            {
                                return currentRank;
                            }
                    }
                }

                return 3;
            }
        }

        // This method takes a target rank and increase/decreases the current rank to match it, taking into account debuffs that need to be added/removed along the way
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

        // Helper method that adds a debuff depending on phobia rank
        private void AddPhobiaDebuff(PhobiaID phobia, int rank)
        {
            PhobiaDebuff debuff = SelectDebuff(phobia, rank);
            playerPhobiaData[phobia].activeDebuffs.Add(debuff);
        }

        // This method randomly selects a phobia debuff depending on the phobia and rank
        public static PhobiaDebuff SelectDebuff(PhobiaID phobia, int rank)
        {
            PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition definition);
            PhobiaDefinition.PhobiaType phobiaType = definition.type;

            List<PhobiaDebuff> typeList = PhobiaDebuffData.typeDebuffs[phobiaType];
            List<PhobiaDebuff> phobiaSpecificList = PhobiaDebuffData.phobiaSpecificDebuffs[phobia];

            List<PhobiaDebuff> typeFiltered = typeList.Where(debuff => debuff.rank == rank).ToList();
            List<PhobiaDebuff> phobiaSpecificFiltered = phobiaSpecificList.Where(debuff => debuff.rank == rank).ToList();

            List<PhobiaDebuff> combinedList = typeFiltered.Concat(phobiaSpecificFiltered).ToList();
            return combinedList[Random.Shared.Next(0, combinedList.Count)];
        }

        // Helper method that remove a debuff depending on phobia rank
        private void RemovePhobiaDebuff(PhobiaID phobia, int rank)
        {
            PhobiaDebuff debuff = playerPhobiaData[phobia].activeDebuffs.FirstOrDefault(activeDebuff => activeDebuff.rank == rank);
            playerPhobiaData[phobia].activeDebuffs.Remove(debuff);
        }

        // Helper method that returns if the player has a specific debuff
        public bool HasDebuff(PhobiaID phobia, PhobiaDebuff.PhobiaDebuffID debuffID)
        {
            if (!playerPhobiaData[phobia].hasPhobia) return false;
            return playerPhobiaData[phobia].activeDebuffs.Any(d => d.id == debuffID);
        }

        // Helper method that determines if there is an active debuff at any specific phobia rank
        public bool HasDebuffAtRank(PhobiaID phobia, int rank)
        {
            if (!playerPhobiaData[phobia].hasPhobia) return false;
            return playerPhobiaData[phobia].activeDebuffs.Any(d => d.rank == rank);
        }
    }
}
