using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ReignOfFear.Content.Systems.FearSystem
{
    /// <summary>
    /// This is a data container that we use to track a multitude of variables that the combat tracker
    /// uses in order to determine when combat starts, ends, and how much courage should be awarded
    /// when a combat instance ends
    /// </summary>

    public class CombatData
    {
        public int npcType;
        public int totalMaxHP = 0;
        public float combatTime;
        public ulong lastDamageFrame;

        public Dictionary<int, int> playerDamageContributions = new Dictionary<int, int>();
        public Dictionary<int, int> damageDealtToPlayers = new Dictionary<int, int>();
        public Dictionary<int, int> moonLordEyeSnapshots = new Dictionary<int, int>();

        public List<int> pairedNPCs = new List<int>();
        public HashSet<int> deadComponents = new HashSet<int>();
    }

    /// <summary>
    /// The combat tracker is single handedly the most important system that drive the Fear System. It
    /// tracks all enemy combat instances, what constitutes as a combat instance, drives logic around
    /// courage accumulation, and allows for us to know exactly what every player/enemy is doing at any given moment
    /// 
    /// Combat risk is determined by three things: the player's active combat time, how much health vs their max health was lost,
    /// and how much damage the player actually did to the threat. Potential courage is calculated by how much damage the player
    /// took added to the amount of time the combat took. Then, courage is awarded depending on how much damage the player did to the enemy
    /// 
    /// Luckily, the 'whoAmI' array makes this tracker possible as it guarantees us a unique key for us to tie to the combat instance
    /// that most other values also happen to run off of (I.E. certain important ai[] values or 'realLife' values). This allows
    /// the combat tracker to accurately separate different instances of combat to ensure the most accurate data for courage
    /// calculations in this sytem
    /// 
    /// Something to note is that the total damage percentage needed changes with the number of participating players,
    /// and the HP of damaged components/destroyed components are added to the total HP of the fight depending on their
    /// relevance in the combat instance (determined by max HP - current HP, unless the part of destroyed in which case
    /// their full max HP is added)
    /// </summary>

    public class CombatTracker : ModSystem
    {
        private static Dictionary<int, CombatData> activeCombats = new Dictionary<int, CombatData>();

        private const float COMBAT_TIMEOUT = 10f;

        // This method is used to update trackers and to end them if NPCs happen to despawn instead of die
        public override void PostUpdateEverything()
        {
            List<int> combatKeys = [..activeCombats.Keys];

            foreach (int npcKey in combatKeys)
            {
                CombatData combat = activeCombats[npcKey];

                NPC npc = FindNPCByKey(npcKey);
                bool shouldRemoveCombat = false;

                if (npc == null || !npc.active)
                {
                    if (combat.pairedNPCs.Count > 0)
                    {
                        bool anyAlive = false;
                        foreach (int pairedWhoAmI in combat.pairedNPCs)
                        {
                            for (int i = 0; i < Main.maxNPCs; i++)
                            {
                                if (Main.npc[i].active && Main.npc[i].whoAmI == pairedWhoAmI)
                                {
                                    anyAlive = true;
                                    break;
                                }
                            }
                            if (anyAlive) break;
                        }

                        if (!anyAlive)
                        {
                            shouldRemoveCombat = true;
                        }
                    }
                    else if (SegmentedBossData.IsGolemType(combat.npcType))
                    {
                        bool anyGolemAlive = false;
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            if (Main.npc[i].active && SegmentedBossData.IsGolemType(Main.npc[i].type))
                            {
                                anyGolemAlive = true;
                                break;
                            }
                        }

                        if (!anyGolemAlive)
                        {
                            shouldRemoveCombat = true;
                        }
                    }
                    else
                    {
                        shouldRemoveCombat = true;
                    }
                }

                if (shouldRemoveCombat)
                {
                    if (combat.pairedNPCs.Count > 0)
                    {
                        SegmentedBossData.UnregisterPairing(npcKey, combat);
                    }
                    activeCombats.Remove(npcKey);
                    continue;
                }

                ulong framesSinceLastDamage = Main.GameUpdateCount - combat.lastDamageFrame;
                float secondsSinceLastDamage = framesSinceLastDamage / 60f;

                if (secondsSinceLastDamage < COMBAT_TIMEOUT)
                {
                    combat.combatTime += 1f / 60f;
                }
            }
        }

        // Helper method that locates specific combat instances via a key (usually the 'whoAmI')
        private static NPC FindNPCByKey(int key)
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];

                if (!npc.active)
                    continue;

                int npcKey = npc.realLife != -1 ? npc.realLife : npc.whoAmI;

                if (npcKey == key)
                    return npc;
            }

            return null;
        }

        // Used to intiate combat instances when a player deals the first hit
        public static void RecordPlayerDamage(NPC npc, int playerIndex, int damage)
        {
            if (!npc.active)
                return;

            bool isMoonLordEye = (npc.type == NPCID.MoonLordHead || npc.type == NPCID.MoonLordHand);

            if (npc.life <= 0 && !isMoonLordEye)
                return;

            int npcKey = npc.whoAmI;
            if (npc.realLife != -1)
            {
                npcKey = npc.realLife;
            }
            else if (SegmentedBossData.IsEaterType(npc.type))
            {
                bool foundExisting = false;
                foreach (var kvp in activeCombats)
                {
                    if (kvp.Value.pairedNPCs.Contains(npc.whoAmI))
                    {
                        npcKey = kvp.Key;
                        foundExisting = true;
                        break;
                    }
                }

                if (!foundExisting)
                {
                    npcKey = npc.whoAmI;

                    bool segmentInstantKilled = (npc.life <= 0);
                    int upperFragmentTail = -1;
                    int lowerFragmentHead = -1;

                    if (segmentInstantKilled)
                    {
                        upperFragmentTail = (int)npc.ai[1];
                        lowerFragmentHead = (int)npc.ai[0];
                    }
                }
            }
            else if (SegmentedBossData.IsGolemType(npc.type))
            {
                if (SegmentedBossData.TryGetGolemCombatKey(activeCombats, out int golemKey))
                {
                    npcKey = golemKey;
                }
                else
                {
                    npcKey = npc.whoAmI;
                }
            }
            else if (SegmentedBossData.IsTwinType(npc.type))
            {
                if (SegmentedBossData.IsNPCPaired(npc.whoAmI, out int existingKey))
                {
                    npcKey = existingKey;
                }
                else
                {
                    npcKey = npc.whoAmI;
                }
            }
            else if (SegmentedBossData.UsesAIGrouping(npc.type, out int aiIndex))
            {
                if (SegmentedBossData.IsAIPrimary(npc.type))
                    npcKey = npc.whoAmI;
                else
                    npcKey = (int)npc.ai[aiIndex];
            }
            else
            {
                npcKey = npc.whoAmI;
            }

            if ((npc.type == NPCID.MoonLordHead || npc.type == NPCID.MoonLordHand) && activeCombats.ContainsKey(npcKey))
            {
                CombatData combat = activeCombats[npcKey];

                int snapshotHP = combat.moonLordEyeSnapshots.ContainsKey(npc.whoAmI)
                    ? combat.moonLordEyeSnapshots[npc.whoAmI]
                    : npc.lifeMax;

                int calculatedHP = snapshotHP - damage;

                if (calculatedHP <= 0)
                {
                    RecordComponentDeath(npc, npcKey);
                    combat.moonLordEyeSnapshots.Remove(npc.whoAmI);
                }
                else
                {
                    combat.moonLordEyeSnapshots[npc.whoAmI] = calculatedHP;
                }
            }

            if (!activeCombats.ContainsKey(npcKey))
            {
                activeCombats[npcKey] = new CombatData
                {
                    npcType = npc.type,
                    combatTime = 0f,
                    lastDamageFrame = Main.GameUpdateCount
                };

                CombatData combat = activeCombats[npcKey];

                if (SegmentedBossData.IsTwinType(npc.type))
                {
                    combat.pairedNPCs.Add(npc.whoAmI);
                    SegmentedBossData.RegisterPairing(npcKey, npc.whoAmI);

                    int oppositeType = SegmentedBossData.GetOppositeTwin(npc.type);
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC other = Main.npc[i];
                        if (!other.active || other.type != oppositeType) continue;
                        if (SegmentedBossData.IsNPCPaired(other.whoAmI, out _)) continue;

                        combat.pairedNPCs.Add(other.whoAmI);
                        SegmentedBossData.RegisterPairing(npcKey, other.whoAmI);
                        break;
                    }
                }
                else if (SegmentedBossData.IsEaterType(npc.type))
                {
                    List<int> allSegments = new List<int>();

                    if (npc.life <= 0)
                    {
                        int upperFragmentTail = (int)npc.ai[1];
                        int lowerFragmentHead = (int)npc.ai[0];

                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            if (!Main.npc[i].active || Main.npc[i].type != NPCID.EaterofWorldsTail) continue;

                            List<int> chain = SegmentedBossData.TraverseEaterChain(Main.npc[i].whoAmI);

                            if (chain.Contains(upperFragmentTail) || chain.Contains(lowerFragmentHead))
                            {
                                allSegments.AddRange(chain);
                            }
                        }

                        combat.totalMaxHP += npc.lifeMax;
                    }
                    else
                    {
                        int currentWhoAmI = npc.whoAmI;
                        while (currentWhoAmI >= 0 && currentWhoAmI < Main.maxNPCs)
                        {
                            NPC current = Main.npc[currentWhoAmI];
                            if (!current.active || !SegmentedBossData.IsEaterType(current.type))
                                break;

                            if (current.type == NPCID.EaterofWorldsTail)
                            {
                                allSegments = SegmentedBossData.TraverseEaterChain(currentWhoAmI);
                                break;
                            }

                            currentWhoAmI = (int)current.ai[0];
                        }
                    }

                    combat.pairedNPCs.AddRange(allSegments);
                }
            }

            CombatData combatData = activeCombats[npcKey];
            combatData.lastDamageFrame = Main.GameUpdateCount;

            if (combatData.playerDamageContributions.ContainsKey(playerIndex))
            {
                combatData.playerDamageContributions[playerIndex] += damage;
            }
            else
            {
                combatData.playerDamageContributions[playerIndex] = damage;
            }
        }

        // Similar to the above method, but instead used to intiate combat instances when an enemy deals the first strike
        public static void RecordEnemyDamage(NPC npc, int playerIndex, int damage)
        {
            if (!npc.active)
                return;

            int npcKey = npc.whoAmI;
            if (npc.realLife != -1)
            {
                npcKey = npc.realLife;
            }
            else if (SegmentedBossData.IsEaterType(npc.type))
            {
                bool foundExisting = false;
                foreach (var kvp in activeCombats)
                {
                    if (kvp.Value.pairedNPCs.Contains(npc.whoAmI))
                    {
                        npcKey = kvp.Key;
                        foundExisting = true;
                        break;
                    }
                }

                if (!foundExisting)
                {
                    npcKey = npc.whoAmI;
                }
            }
            else if (SegmentedBossData.IsGolemType(npc.type))
            {
                if (SegmentedBossData.TryGetGolemCombatKey(activeCombats, out int golemKey))
                {
                    npcKey = golemKey;
                }
                else
                {
                    npcKey = npc.whoAmI;
                }
            }
            else if (SegmentedBossData.IsTwinType(npc.type))
            {
                if (SegmentedBossData.IsNPCPaired(npc.whoAmI, out int existingKey))
                {
                    npcKey = existingKey;
                }
                else
                {
                    npcKey = npc.whoAmI;
                }
            }
            else if (SegmentedBossData.UsesAIGrouping(npc.type, out int aiIndex))
            {
                if (SegmentedBossData.IsAIPrimary(npc.type))
                    npcKey = npc.whoAmI;
                else
                    npcKey = (int)npc.ai[aiIndex];
            }
            else
            {
                npcKey = npc.whoAmI;
            }

            if (!activeCombats.ContainsKey(npcKey))
            {
                activeCombats[npcKey] = new CombatData
                {
                    npcType = npc.type,
                    combatTime = 0f,
                    lastDamageFrame = Main.GameUpdateCount
                };

                CombatData combat = activeCombats[npcKey];

                if (SegmentedBossData.IsTwinType(npc.type))
                {
                    combat.pairedNPCs.Add(npc.whoAmI);
                    SegmentedBossData.RegisterPairing(npcKey, npc.whoAmI);

                    int oppositeType = SegmentedBossData.GetOppositeTwin(npc.type);
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC other = Main.npc[i];
                        if (!other.active || other.type != oppositeType) continue;
                        if (SegmentedBossData.IsNPCPaired(other.whoAmI, out _)) continue;

                        combat.pairedNPCs.Add(other.whoAmI);
                        SegmentedBossData.RegisterPairing(npcKey, other.whoAmI);
                        break;
                    }
                }
                else if (SegmentedBossData.IsEaterType(npc.type))
                {
                    List<int> chain = SegmentedBossData.TraverseEaterChain(npc.whoAmI);
                    combat.pairedNPCs.AddRange(chain);
                }
            }

            CombatData combatData = activeCombats[npcKey];
            combatData.lastDamageFrame = Main.GameUpdateCount;

            if (combatData.damageDealtToPlayers.ContainsKey(playerIndex))
            {
                combatData.damageDealtToPlayers[playerIndex] += damage;
            }
            else
            {
                combatData.damageDealtToPlayers[playerIndex] = damage;
            }
        }

        // Helper method that tracks when a segmented enemy's component is destroyed, but not the core
        public static void RecordComponentDeath(NPC npc, int combatKey)
        {
            if (!activeCombats.ContainsKey(combatKey))
            {
                return;
            }

            CombatData combat = activeCombats[combatKey];
            combat.totalMaxHP += npc.lifeMax;
        }

        // This method runs when an enemy dies (or the core of a segmented enemy dies) which allows us to end the combat instance and award courage
        public static void OnEnemyKilled(NPC npc)
        {
            int npcKey = npc.whoAmI;
            if (npc.realLife != -1)
                npcKey = npc.realLife;
            else if (SegmentedBossData.IsEaterType(npc.type))
            {
                bool foundExisting = false;
                foreach (var kvp in activeCombats)
                {
                    if (kvp.Value.pairedNPCs.Contains(npc.whoAmI))
                    {
                        npcKey = kvp.Key;
                        foundExisting = true;
                        break;
                    }
                }

                if (!foundExisting)
                {
                    npcKey = npc.whoAmI;
                }
            }
            else if (SegmentedBossData.IsGolemType(npc.type))
            {
                if (SegmentedBossData.TryGetGolemCombatKey(activeCombats, out int golemKey))
                {
                    npcKey = golemKey;
                }
                else
                {
                    npcKey = npc.whoAmI;
                }
            }
            else if (SegmentedBossData.IsTwinType(npc.type))
            {
                if (SegmentedBossData.IsNPCPaired(npc.whoAmI, out int existingKey))
                    npcKey = existingKey;
                else
                    npcKey = npc.whoAmI;
            }
            else if (SegmentedBossData.UsesAIGrouping(npc.type, out int aiIndex))
            {
                if (SegmentedBossData.IsAIPrimary(npc.type))
                    npcKey = npc.whoAmI;
                else
                    npcKey = (int)npc.ai[aiIndex];
            }
            else
                npcKey = npc.whoAmI;

            if (!activeCombats.ContainsKey(npcKey))
            {
                return;
            }

            CombatData combat = activeCombats[npcKey];
            RecordComponentDeath(npc, npcKey);
            combat.deadComponents.Add(npc.whoAmI);
            bool shouldAwardCourage = false;

            if (SegmentedBossData.IsGolemType(npc.type))
            {
                bool isCoreDestroyed = (npc.type == NPCID.Golem);

                if (!isCoreDestroyed)
                {
                    return;
                }

                shouldAwardCourage = true;

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC otherNPC = Main.npc[i];
                    if (!otherNPC.active) continue;
                    if (otherNPC.whoAmI == npc.whoAmI) continue;
                    if (!SegmentedBossData.IsGolemType(otherNPC.type)) continue;

                    int damageTaken = otherNPC.lifeMax - otherNPC.life;
                    combat.totalMaxHP += damageTaken;
                }
            }
            else if (SegmentedBossData.IsEaterType(npc.type))
            {
                bool allDead = combat.pairedNPCs.All(whoAmI => combat.deadComponents.Contains(whoAmI));

                if (allDead)
                {
                    shouldAwardCourage = true;
                }
                else
                {
                    return;
                }
            }
            else if (SegmentedBossData.IsTwinType(npc.type))
            {
                bool allDead = combat.pairedNPCs.All(whoAmI => combat.deadComponents.Contains(whoAmI));

                if (allDead)
                {
                    shouldAwardCourage = true;
                }
                else
                {
                    return;
                }
            }
            else if (SegmentedBossData.UsesAIGrouping(npc.type, out int groupAiIndex))
            {
                bool isPrimary = SegmentedBossData.IsAIPrimary(npc.type);

                if (!isPrimary)
                {
                    return;
                }

                shouldAwardCourage = true;

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC otherNPC = Main.npc[i];
                    if (!otherNPC.active) continue;
                    if (otherNPC.whoAmI == npc.whoAmI) continue;
                    if (!SegmentedBossData.UsesAIGrouping(otherNPC.type, out int otherAiIndex)) continue;
                    if ((int)otherNPC.ai[groupAiIndex] != npc.whoAmI) continue;

                    int damageTaken = otherNPC.lifeMax - otherNPC.life;
                    combat.totalMaxHP += damageTaken;
                }
            }
            else
            {
                shouldAwardCourage = true;
            }

            if (shouldAwardCourage)
            {
                if (combat.totalMaxHP <= 0)
                {
                    combat.totalMaxHP = 1;
                }

                foreach (int playerIndex in combat.playerDamageContributions.Keys)
                {
                    Player player = Main.player[playerIndex];
                    if (player == null || !player.active)
                        continue;

                    int courageAwarded = CalculateCourage(combat, player, combat.totalMaxHP);

                    if (PhobiaData.NPCPhobiaMap.TryGetValue(npc.type, out List<PhobiaID> phobias))
                    {
                        FearSystemPlayer modPlayer = player.GetModPlayer<FearSystemPlayer>();
                        foreach (PhobiaID phobia in phobias)
                        {
                            modPlayer.AddCouragePoints(phobia, courageAwarded);
                        }
                    }
                }

                if (combat.pairedNPCs.Count > 0)
                {
                    SegmentedBossData.UnregisterPairing(npcKey, combat);
                }

                activeCombats.Remove(npcKey);
            }
        }

        // This method calculates how much courage the player should receive when combat ends
        public static int CalculateCourage(CombatData combat, Player player, int enemyMaxHP)
        {
            float timePoints = Math.Min(50f, (combat.combatTime / 180f) * 50f);

            float totalDamageTaken = combat.damageDealtToPlayers.ContainsKey(player.whoAmI)
                ? combat.damageDealtToPlayers[player.whoAmI]
                : 0f;

            float normalizedDamage = totalDamageTaken * (100f / player.statLifeMax2);
            float damagePoints = Math.Min(50f, normalizedDamage / 3f);

            float potentialCourage = timePoints + damagePoints;

            int playerDamage = combat.playerDamageContributions.ContainsKey(player.whoAmI)
                ? combat.playerDamageContributions[player.whoAmI]
                : 0;

            if (enemyMaxHP <= 0)
            {
                playerDamage = 1;
                enemyMaxHP = 1;
            }

            float damagePercentage = (float)playerDamage / enemyMaxHP;
            damagePercentage = Math.Min(1.0f, damagePercentage);
            int participatingPlayers = combat.playerDamageContributions.Count;
            float playerThreshold = 0.5f / participatingPlayers;

            float finalCourage = damagePercentage >= playerThreshold
                ? potentialCourage
                : potentialCourage * (damagePercentage / playerThreshold);

            return (int)Math.Floor(finalCourage);
        }

        // This method wipes the 'potential courage' of all combat instances a dead player was involved in (without removing their existence in the combat instance)
        public static void OnPlayerDeath(int playerIndex)
        {
            foreach (CombatData combat in activeCombats.Values)
            {
                if (combat.damageDealtToPlayers.ContainsKey(playerIndex))
                {
                    combat.damageDealtToPlayers[playerIndex] = 0;
                }

                combat.combatTime = 0f;
                combat.lastDamageFrame = Main.GameUpdateCount - (ulong)(COMBAT_TIMEOUT * 2 * 60);
            }
        }
    }
}
