using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace ReignOfFear.Content.Systems.FearSystem
{
    /// <summary>
    /// This file is a container for all the data centered around phobia sets
    /// and the passive effect it applies to both the player and NPCs in the game
    /// depending on the rank of the phobia sets that are in the mod
    /// </summary>
    /// 
    public static class PhobiaSetEffects
    {
        public const int P_CAP_NORMAL = 30;
        public const int P_CAP_EXPERT = 20;
        public const int P_CAP_MASTER = 10;

        public const float HP_CAP_NONBOSS = 0.50f;
        public const float DMG_CAP_NONBOSS = 1.00f;
        public const float DEF_CAP_NONBOSS = 0.20f;
        public const float KB_CAP_NONBOSS = 0.15f;

        public const float HP_CAP_BOSS = 0.30f;
        public const float DMG_CAP_BOSS = 1.00f;
        public const float DEF_CAP_BOSS = 0.20f;

        public const float CONTEXTUAL_FRACTION = 0.30f;
        private const float DARKNESS_THRESHOLD = 0.15f;

        public static bool DebugEnabled = true;

        public struct Bonus
        {
            public float hp, dmg, def, kb;
            public bool IsZero => hp == 0f && dmg == 0f && def == 0f && kb == 0f;
        }

        public static int GetPhobiaCap()
        {
            if (Main.masterMode) return P_CAP_MASTER;
            if (Main.expertMode) return P_CAP_EXPERT;
            return P_CAP_NORMAL;
        }

        public static float GetProgression(int phobiaCount)
        {
            float ratio = Math.Min((float)phobiaCount / GetPhobiaCap(), 1f);
            return ratio * ratio;
        }

        public static Bonus ComputeEnemyBonus(NPC npc, Player player)
        {
            Bonus result = default;
            if (!PhobiaData.NPCPhobiaMap.TryGetValue(npc.type, out List<PhobiaID> phobias))
                return result;

            FearSystemPlayer mp = player.GetModPlayer<FearSystemPlayer>();
            int totalPhobias = mp.GetTotalPhobiaCount();
            if (totalPhobias <= 0) return result;

            HashSet<SetID> candidateSets = new HashSet<SetID>();
            foreach (PhobiaID phobia in phobias)
                if (PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition def))
                    candidateSets.Add(def.set);

            int bestRank = 0;
            foreach (SetID setID in candidateSets)
            {
                int rank = mp.GetSetRank(setID);
                if (rank > bestRank) bestRank = rank;
            }

            float progression = GetProgression(totalPhobias);
            bool isBoss = IsBoss(npc);

            if (bestRank > 0)
            {
                float rankScalar = bestRank / 3f;
                if (isBoss)
                {
                    result.hp = progression * rankScalar * HP_CAP_BOSS;
                    result.dmg = progression * rankScalar * DMG_CAP_BOSS;
                    result.def = progression * rankScalar * DEF_CAP_BOSS;
                }
                else
                {
                    result.hp = progression * rankScalar * HP_CAP_NONBOSS;
                    result.dmg = progression * rankScalar * DMG_CAP_NONBOSS;
                    result.def = progression * rankScalar * DEF_CAP_NONBOSS;
                    result.kb = progression * rankScalar * KB_CAP_NONBOSS;
                }
            }

            int bestCtxRank = GetBestContextRank(player, mp);
            if (bestCtxRank > 0)
            {
                float scale = progression * (bestCtxRank / 3f) * CONTEXTUAL_FRACTION;
                if (isBoss)
                {
                    result.hp += scale * HP_CAP_BOSS;
                    result.dmg += scale * DMG_CAP_BOSS;
                    result.def += scale * DEF_CAP_BOSS;
                }
                else
                {
                    result.hp += scale * HP_CAP_NONBOSS;
                    result.dmg += scale * DMG_CAP_NONBOSS;
                    result.def += scale * DEF_CAP_NONBOSS;
                    result.kb += scale * KB_CAP_NONBOSS;
                }
            }

            if (DebugEnabled && !result.IsZero)
                Log($"{npc.TypeName} p:{totalPhobias} r:{bestRank} ctx:{bestCtxRank} " +
                    $"prog:{progression:F2} hp+{result.hp:P0} dmg+{result.dmg:P0} " +
                    $"def+{result.def:P0} kb+{result.kb:P0}");

            return result;
        }

        private static int GetBestContextRank(Player player, FearSystemPlayer mp)
        {
            int best = 0;
            void Check(SetID s) { int r = mp.GetSetRank(s); if (r > best) best = r; }

            if (HasAnyHarmfulDebuff(player)) Check(SetID.Afflictions);
            if (player.ZoneOverworldHeight || player.ZoneSkyHeight) Check(SetID.Nature);
            if (IsInDarkness(player)) Check(SetID.Underground);
            if (player.wet && !player.lavaWet && !player.honeyWet) Check(SetID.Ocean);
            if (player.lavaWet) Check(SetID.Hell);
            return best;
        }

        private static bool HasAnyHarmfulDebuff(Player player)
        {
            for (int i = 0; i < Player.MaxBuffs; i++)
            {
                int t = player.buffType[i];
                if (t > 0 && Main.debuff[t]) return true;
            }
            return false;
        }

        private static bool IsInDarkness(Player player)
        {
            int tx = (int)(player.Center.X / 16f);
            int ty = (int)(player.Center.Y / 16f);
            Color c = Lighting.GetColor(tx, ty);
            float brightness = (c.R + c.G + c.B) / (3f * 255f);
            return brightness < DARKNESS_THRESHOLD;
        }

        private static bool IsBoss(NPC npc)
        {
            if (npc.boss) return true;
            if (NPCID.Sets.BossHeadTextures[npc.type] >= 0) return true;

            if (SegmentedBossData.IsEaterType(npc.type)) return true;
            if (SegmentedBossData.IsBrainType(npc.type)) return true;
            if (SegmentedBossData.IsTwinType(npc.type)) return true;
            if (SegmentedBossData.IsPlanteraType(npc.type)) return true;
            if (SegmentedBossData.IsGolemType(npc.type)) return true;
            if (SegmentedBossData.UsesAIGrouping(npc.type, out _)) return true;

            return false;
        }

        public static void Log(string msg)
        {
            if (!DebugEnabled || Main.netMode == NetmodeID.Server) return;
            Main.NewText("[Fear] " + msg, Color.Orange);
        }
    }
}