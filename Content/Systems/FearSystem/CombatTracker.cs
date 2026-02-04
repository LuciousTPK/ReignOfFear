using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace ReignOfFear.Content.Systems.FearSystem
{
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
                if (npc == null || !npc.active)
                {
                    activeCombats.Remove(npcKey);
                    continue;
                }

                ulong framesSinceLastDamage = Main.GameUpdateCount - combat.lastDamageFrame;
                float secondsSinceLastDamage = framesSinceLastDamage / 60f;

                if (secondsSinceLastDamage < COMBAT_TIMEOUT)
                {
                    combat.combatTime += 1f / 60f;
                }

                if (secondsSinceLastDamage > COMBAT_TIMEOUT * 2)
                {
                    activeCombats.Remove(npcKey);
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

            Main.NewText($"[COMBAT] NPC {npc.type}: realLife={npc.realLife}", Color.Orange);

            Main.NewText($"[COMBAT] Type {npc.type}, whoAmI={npc.whoAmI}, ai[0]={npc.ai[0]}, ai[1]={npc.ai[1]}", Color.Orange);

            int npcKey = npc.realLife != -1 ? npc.realLife : npc.whoAmI;

            if (!activeCombats.ContainsKey(npcKey))
            {
                activeCombats[npcKey] = new CombatData
                {
                    npcType = npc.type,
                    combatTime = 0f,
                    lastDamageFrame = Main.GameUpdateCount
                };
            }

            CombatData combat = activeCombats[npcKey];
            combat.lastDamageFrame = Main.GameUpdateCount;

            if (combat.playerDamageContributions.ContainsKey(playerIndex))
            {
                combat.playerDamageContributions[playerIndex] += damage;
            }
            else
            {
                combat.playerDamageContributions[playerIndex] = damage;
            }
        }

        public static void RecordEnemyDamage(NPC npc, int playerIndex, int damage)
        {
            if (!npc.active)
                return;

            int npcKey = npc.realLife != -1 ? npc.realLife : npc.whoAmI;

            if (!activeCombats.ContainsKey(npcKey))
            {
                activeCombats[npcKey] = new CombatData
                {
                    npcType = npc.type,
                    combatTime = 0f,
                    lastDamageFrame = Main.GameUpdateCount
                };
            }

            CombatData combat = activeCombats[npcKey];
            combat.lastDamageFrame = Main.GameUpdateCount;

            if (combat.damageDealtToPlayers.ContainsKey(playerIndex))
            {
                combat.damageDealtToPlayers[playerIndex] += damage;
            }
            else
            {
                combat.damageDealtToPlayers[playerIndex] = damage;
            }
        }

        public static void OnEnemyKilled(NPC npc)
        {
            int npcKey = npc.realLife != -1 ? npc.realLife : npc.whoAmI;

            Main.NewText($"[COMBAT] NPC {npc.type}: ai[0]={npc.ai[0]}, ai[1]={npc.ai[1]}", Color.Orange);

            if (!activeCombats.ContainsKey(npcKey))
            {
                return;
            }

            CombatData combat = activeCombats[npcKey];

            foreach (int playerIndex in combat.playerDamageContributions.Keys)
            {
                Player player = Main.player[playerIndex];

                if (player == null || !player.active)
                    continue;

                int courageAwarded = CalculateCourage(combat, player, npc.lifeMax);

                if (PhobiaData.NPCPhobiaMap.TryGetValue(npc.type, out List<PhobiaID> phobias))
                {
                    FearSystemPlayer modPlayer = player.GetModPlayer<FearSystemPlayer>();
                    foreach (PhobiaID phobia in phobias)
                    {
                        modPlayer.AddCouragePoints(phobia, courageAwarded);
                    }
                }
            }

            activeCombats.Remove(npcKey);
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

            float damagePercentage = (float)playerDamage / enemyMaxHP;
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

    public class CombatData
    {
        public int npcType;
        public float combatTime;
        public ulong lastDamageFrame;

        public Dictionary<int, int> playerDamageContributions = new Dictionary<int, int>();
        public Dictionary<int, int> damageDealtToPlayers = new Dictionary<int, int>();
    }
}
