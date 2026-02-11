using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace ReignOfFear.Content.Systems.FearSystem
{
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

    public class CombatTracker : ModSystem
    {
        private static Dictionary<int, CombatData> activeCombats = new Dictionary<int, CombatData>();

        private const float COMBAT_TIMEOUT = 10f;

        public override void PostUpdateEverything()
        {
            List<int> combatKeys = new List<int>(activeCombats.Keys);

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

        public static void RecordPlayerDamage(NPC npc, int playerIndex, int damage)
        {
            if (!npc.active)
                return;

            bool isMoonLordEye = (npc.type == 396 || npc.type == 397);

            if (npc.life <= 0 && !isMoonLordEye)
                return;

            if (npc.type >= 245 && npc.type <= 249)
            {
                Main.NewText($"=== GOLEM DEBUG (Player hit Type {npc.type}) ===", Color.Yellow);
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC other = Main.npc[i];
                    if (!other.active) continue;
                    if (other.type < 245 || other.type > 249) continue;
                    Main.NewText($"Type {other.type}, whoAmI={other.whoAmI}: ai[0]={other.ai[0]}, ai[1]={other.ai[1]}, ai[2]={other.ai[2]}, ai[3]={other.ai[3]}", Color.Cyan);
                }
            }

            int npcKey;
            if (npc.realLife != -1)
            {
                npcKey = npc.realLife;
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

            if ((npc.type == 396 || npc.type == 397) && activeCombats.ContainsKey(npcKey))
            {
                CombatData combat = activeCombats[npcKey];

                int snapshotHP = combat.moonLordEyeSnapshots.ContainsKey(npc.whoAmI)
                    ? combat.moonLordEyeSnapshots[npc.whoAmI]
                    : npc.lifeMax;

                int calculatedHP = snapshotHP - damage;

                Main.NewText($"[ML SNAP] Type {npc.type}: snapshot={snapshotHP}, damage={damage}, calculated={calculatedHP}", Color.Purple);

                if (calculatedHP <= 0)
                {
                    Main.NewText($"[ML DEATH] Eye died, recording {npc.lifeMax} HP", Color.Purple);
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

        public static void RecordEnemyDamage(NPC npc, int playerIndex, int damage)
        {
            if (!npc.active)
                return;

            int npcKey;
            if (npc.realLife != -1)
            {
                npcKey = npc.realLife;
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

        public static void RecordComponentDeath(NPC npc, int combatKey)
        {
            if (!activeCombats.ContainsKey(combatKey))
                return;

            CombatData combat = activeCombats[combatKey];
            combat.totalMaxHP += npc.lifeMax;
        }

        public static void OnEnemyKilled(NPC npc)
        {
            int npcKey;
            if (npc.realLife != -1)
                npcKey = npc.realLife;
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

            Main.NewText($"[KILL] Type={npc.type}, whoAmI={npc.whoAmI}, Key={npcKey}", Color.Red);

            if (!activeCombats.ContainsKey(npcKey))
            {
                Main.NewText($"[KILL] Not tracked, ignoring", Color.Red);
                return;
            }

            CombatData combat = activeCombats[npcKey];

            RecordComponentDeath(npc, npcKey);
            combat.deadComponents.Add(npc.whoAmI);
            bool shouldAwardCourage = false;

            if (SegmentedBossData.IsGolemType(npc.type))
            {
                bool isCoreDestroyed = (npc.type == 245);

                if (!isCoreDestroyed)
                {
                    Main.NewText($"[KILL] Golem component died, waiting for core", Color.Orange);
                    return;
                }

                Main.NewText($"[KILL] Golem core died, processing courage", Color.Orange);
                shouldAwardCourage = true;

                // Add surviving component HP
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC otherNPC = Main.npc[i];
                    if (!otherNPC.active) continue;
                    if (otherNPC.whoAmI == npc.whoAmI) continue;
                    if (!SegmentedBossData.IsGolemType(otherNPC.type)) continue;

                    int damageTaken = otherNPC.lifeMax - otherNPC.life;
                    combat.totalMaxHP += damageTaken;
                    Main.NewText($"[HP TRACK] Surviving component {otherNPC.type}: +{damageTaken} damage taken", Color.Magenta);
                }
            }
            else if (SegmentedBossData.IsTwinType(npc.type))
            {
                bool allDead = combat.pairedNPCs.All(whoAmI => combat.deadComponents.Contains(whoAmI));

                if (allDead)
                {
                    Main.NewText($"[KILL] All twins dead, awarding courage", Color.Orange);
                    shouldAwardCourage = true;
                }
                else
                {
                    Main.NewText($"[KILL] Twin died, waiting for pair ({combat.deadComponents.Count}/{combat.pairedNPCs.Count})", Color.Orange);
                    return;
                }
            }
            else if (SegmentedBossData.UsesAIGrouping(npc.type, out int groupAiIndex))
            {
                bool isPrimary = SegmentedBossData.IsAIPrimary(npc.type);

                if (!isPrimary)
                {
                    Main.NewText($"[KILL] Non-primary component died, not awarding courage yet", Color.Orange);
                    return;
                }

                Main.NewText($"[KILL] Primary component died, processing courage", Color.Orange);
                shouldAwardCourage = true;

                // Add surviving component HP
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC otherNPC = Main.npc[i];
                    if (!otherNPC.active) continue;
                    if (otherNPC.whoAmI == npc.whoAmI) continue;
                    if (!SegmentedBossData.UsesAIGrouping(otherNPC.type, out int otherAiIndex)) continue;
                    if ((int)otherNPC.ai[groupAiIndex] != npc.whoAmI) continue;

                    int damageTaken = otherNPC.lifeMax - otherNPC.life;
                    combat.totalMaxHP += damageTaken;
                    Main.NewText($"[HP TRACK] Surviving component {otherNPC.type}: +{damageTaken} damage taken", Color.Magenta);
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
                    Main.NewText($"[COURAGE] Warning: totalMaxHP is {combat.totalMaxHP}, defaulting to 1", Color.Red);
                    combat.totalMaxHP = 1;
                }

                Main.NewText($"[FINAL HP] Combat instance total max HP: {combat.totalMaxHP}", Color.Green);

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
