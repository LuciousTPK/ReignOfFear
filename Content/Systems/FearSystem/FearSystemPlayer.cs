using Microsoft.Xna.Framework;
using ReignOfFear.Content.Systems.FearSystem.PlayerDebuffs;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ReignOfFear.Content.Systems.FearSystem
{
    // Defines phobias based on their name--acts as a key for PhobiaData and PlayerPhobiaStates
    public enum PhobiaID
    {
        // Enemy Phobias
        Kinemortophobia,      // Fear of Zombies
        Phasmophobia,         // Fear of Spirits
        Osteonecrophobia,     // Fear of Skeletons
        Entomophobia,         // Fear of Insectoids
        Scoleciphobia,        // Fear of Worms
        Anthropophobia,       // Fear of Humanoids
        Mechanophobia,        // Fear of Constructs
        Allotriophobia,       // Fear of Aliens
        Myxophobia,           // Fear of Slimes
        Botanophobia,         // Fear of Plants
        Stoicheiophobia,      // Fear of Elementals
        Ichthyophobia,        // Fear of Marine Beasts
        Zoophobia,            // Fear of Beasts/Animals
        Ornithophobia,        // Fear of Avians
        Daemonophobia,        // Fear of Demons
        Sarcophobia,          // Fear of Aberrations
        Arachnophobia,        // Fear of Arachnids

        // Boss Phobias
        Toichossarkasphobia,        // Fear of Wall of Flesh
        Didymamatiaphobia,          // Fear of The Twins
        Skoulikikatastrofeaphobia,  // Fear of The Destroyer
        Metallikokraniophobia,      // Fear of Skeletron Prime
        Iptamenokraniophobia,       // Fear of Skeletron
        Agiasmeniglitsaphobia,      // Fear of Queen Slime
        Vasilikimelissaphobia,      // Fear of Queen Bee
        Sarkofagofytophobia,        // Fear of Plantera
        Seliniakostheosphobia,      // Fear of Moon Lord
        Parafronproskynitisphobia,  // Fear of Lunatic Cultist
        Vasilikiglitsaphobia,       // Fear of King Slime
        Petrinimichaniphobia,       // Fear of Golem
        Matiterasphobia,            // Fear of Eye of Cthulhu
        Theatoufotosphobia,         // Fear of Empress of Light
        Katanalotisplanitiphobia,   // Fear of Eater of Worlds
        Psarigourouniphobia,        // Fear of Duke Fishron
        Kykloptikoelafiphobia,      // Fear of Deerclops
        Tromaktikomyalophobia,      // Fear of Brain of Cthulhu

        // Biome Phobias
        Archaioereipiophobia,  // Fear of Temple
        Chioniphobia,          // Fear of Snow Biome
        Thalassaphobia,        // Fear of Ocean Biome
        Mycophobia,            // Fear of Mushroom Biome
        Zounklaphobia,         // Fear of Jungle Biome
        Stygiophobia,          // Fear of Hell Biome
        Photophobia,           // Fear of Hallow Biome
        Hylophobia,            // Fear of Forest Biome
        Katakomvesphobia,      // Fear of Dungeon Biome
        Ammophobia,            // Fear of Desert Biome
        Hemophobia,            // Fear of Crimson Biome
        Kakigiphobia,          // Fear of Corruption Biome
        Speluncaphobia,        // Fear of Cavern Biome

        // Event Phobias
        Kakofengariphobia,          // Fear of Blood Moon
        Psychrosstratosphobia,      // Fear of Frost Legion
        Pagomenofengariphobia,      // Fear of Frost Moon
        Eisvolikalikantzaronphobia, // Fear of Goblin Army
        Seliniakieisvoliphobia,     // Fear of Lunar Event
        Exogiiniparafrosyniphobia,  // Fear of Martian Madness
        Archaiosstratosphobia,      // Fear of Old One's Army
        Peiratikiepithesiphobia,    // Fear of Pirate Invasion
        Apokosmofengariphobia,      // Fear of Pumpkin Moon
        Ombrophobia,                // Fear of Rain
        Ammothyellaphobia,          // Fear of Sandstorm
        Gloiovrochiaphobia,         // Fear of Slime Rain
        Kosmikophobia,              // Fear of Solar Eclipse

        // Debuff Phobias
        Spasmeniamynaphobia,   // Fear of Armor Debuffs
        Adynamiaphobia,        // Fear of Damage Debuffs
        Meionektimaphobia,     // Fear of Disability Debuffs
        Ponosphobia,           // Fear of Harmful Debuffs
        Argosphobia,           // Fear of Movement Debuffs

        // Environmental Phobias
        Skotadiphobia,   // Fear of Darkness
        Imeraphobia,     // Fear of Day
        Ygrifotiaphobia, // Fear of Lava
        Nychtaphobia,    // Fear of Night
        Nerophobia,      // Fear of Water
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

        // Timer for hazards such as enviromental dangers and DOTs
        private int hazardTicker = 0;

        // Roughly the kill speed of lava and drowning with their timer
        private const int DROWNING_DPS = 16;
        private const int LAVA_DPS = 66;

        // Constants revolving around invasion event location and the event/boss timer
        private const int EVENT_BOSS_FEAR_PER_SECOND = 2;
        private const int INVASION_HORIZONTAL_TILES = 187;
        private const int INVASION_VERTICAL_TILES = 67;
        private int eventBossTicker = 0;

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

        // Currently used in order to update phobia progression that occurs via intervals and update interval timers
        public override void PostUpdate()
        {
            hazardTicker++;
            eventBossTicker++;

            if (hazardTicker % 60 == 0)
            {
                if (Player.wet && !Player.lavaWet && !Player.honeyWet
                    && Player.breath < Player.breathMax
                    && Player.breath > 0)
                {
                    int drowningFear = Math.Max(1, CalculateFearProgression(
                        DROWNING_DPS, Player.statLifeMax2, Player.statLife));
                    AddFearPoints(PhobiaID.Nerophobia, drowningFear);
                }

                if (Player.lavaWet && !Player.lavaImmune)
                {
                    int lavaFear = Math.Max(1, CalculateFearProgression(
                        LAVA_DPS, Player.statLifeMax2, Player.statLife));
                    AddFearPoints(PhobiaID.Ygrifotiaphobia, lavaFear);
                }

                int totalDotDPS = 0;

                if (Player.HasBuff(BuffID.Poisoned))
                    totalDotDPS += 2;

                if (Player.HasBuff(BuffID.OnFire))
                    totalDotDPS += 4;

                if (Player.HasBuff(BuffID.CursedInferno))
                    totalDotDPS += 12;

                if (Player.HasBuff(BuffID.Frostburn))
                    totalDotDPS += 8;

                if (Player.HasBuff(BuffID.Venom))
                    totalDotDPS += 16;

                if (Player.HasBuff(BuffID.Suffocation))
                    totalDotDPS += 20;

                if (Player.HasBuff(BuffID.Electrified))
                    totalDotDPS += Math.Abs(Player.velocity.X) > 0.1f ? 20 : 4;

                if (Player.HasBuff(BuffID.Starving))
                    totalDotDPS += (int)(Player.statLifeMax2 * 0.02f);

                if (totalDotDPS > 0)
                {
                    int dotFear = Math.Max(1, CalculateFearProgression(
                        totalDotDPS, Player.statLifeMax2, Player.statLife));
                    AddFearPoints(PhobiaID.Ponosphobia, dotFear);
                }
            }

            if (eventBossTicker % 60 == 0)
            {
                CheckEventTerrorRadius();
                CheckBossTerrorRadius();
            }
        }

        // Event tracker to see if the player is in an event's terror radius
        private void CheckEventTerrorRadius()
        {
            bool atSurface = Player.ZoneOverworldHeight || Player.ZoneSkyHeight;
            bool inInvasionRange = IsPlayerInInvasionRange();

            if (atSurface)
            {
                if (Main.bloodMoon)
                    AddFearPoints(PhobiaID.Kakofengariphobia, EVENT_BOSS_FEAR_PER_SECOND);

                if (Main.raining)
                    AddFearPoints(PhobiaID.Ombrophobia, EVENT_BOSS_FEAR_PER_SECOND);

                if (Main.eclipse)
                    AddFearPoints(PhobiaID.Kosmikophobia, EVENT_BOSS_FEAR_PER_SECOND);

                if (Main.slimeRain)
                    AddFearPoints(PhobiaID.Gloiovrochiaphobia, EVENT_BOSS_FEAR_PER_SECOND);
            }

            if (Player.ZoneOverworldHeight && Player.ZoneSandstorm)
                AddFearPoints(PhobiaID.Ammothyellaphobia, EVENT_BOSS_FEAR_PER_SECOND);

            if (Player.ZoneOverworldHeight)
            {
                if (Main.pumpkinMoon)
                    AddFearPoints(PhobiaID.Apokosmofengariphobia, EVENT_BOSS_FEAR_PER_SECOND);

                if (Main.snowMoon)
                    AddFearPoints(PhobiaID.Pagomenofengariphobia, EVENT_BOSS_FEAR_PER_SECOND);
            }

            if (Player.ZoneOldOneArmy)
                AddFearPoints(PhobiaID.Archaiosstratosphobia, EVENT_BOSS_FEAR_PER_SECOND);

            if (Player.ZoneTowerNebula || Player.ZoneTowerSolar
                || Player.ZoneTowerStardust || Player.ZoneTowerVortex)
                AddFearPoints(PhobiaID.Seliniakieisvoliphobia, EVENT_BOSS_FEAR_PER_SECOND);

            if (Main.invasionType != 0 && Main.invasionSize > 0 && inInvasionRange)
            {
                switch (Main.invasionType)
                {
                    case InvasionID.GoblinArmy:
                        AddFearPoints(PhobiaID.Eisvolikalikantzaronphobia, EVENT_BOSS_FEAR_PER_SECOND);
                        break;

                    case InvasionID.SnowLegion:
                        AddFearPoints(PhobiaID.Psychrosstratosphobia, EVENT_BOSS_FEAR_PER_SECOND);
                        break;

                    case InvasionID.PirateInvasion:
                        AddFearPoints(PhobiaID.Peiratikiepithesiphobia, EVENT_BOSS_FEAR_PER_SECOND);
                        break;

                    case InvasionID.MartianMadness:
                        AddFearPoints(PhobiaID.Exogiiniparafrosyniphobia, EVENT_BOSS_FEAR_PER_SECOND);
                        break;
                }
            }
        }

        // Boss tracker to see if a player is in it's terror radius
        private void CheckBossTerrorRadius()
        {
            Rectangle detectionBounds = new Rectangle(
                (int)Main.screenPosition.X,
                (int)Main.screenPosition.Y,
                Main.screenWidth,
                Main.screenHeight);
            detectionBounds.Inflate(5000, 5000);

            HashSet<PhobiaID> tickedPhobias = null;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];

                if (!npc.active)
                    continue;

                if (!npc.boss && NPCID.Sets.BossHeadTextures[npc.type] < 0)
                    continue;

                if (!npc.Hitbox.Intersects(detectionBounds))
                    continue;

                if (!PhobiaData.NPCPhobiaMap.TryGetValue(npc.type, out List<PhobiaID> phobias))
                    continue;

                if (tickedPhobias == null)
                    tickedPhobias = new HashSet<PhobiaID>();

                foreach (PhobiaID phobia in phobias)
                {
                    if (!PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition def))
                        continue;

                    if (def.type != PhobiaDefinition.PhobiaType.Boss)
                        continue;

                    if (!tickedPhobias.Add(phobia))
                        continue;

                    AddFearPoints(phobia, EVENT_BOSS_FEAR_PER_SECOND);
                }
            }
        }

        // Checks if the player is actively in an event zone near spawn
        private bool IsPlayerInInvasionRange()
        {
            int playerTileX = (int)(Player.Center.X / 16f);
            int playerTileY = (int)(Player.Center.Y / 16f);

            return Math.Abs(playerTileX - Main.spawnTileX) <= INVASION_HORIZONTAL_TILES
                   && playerTileY <= Main.worldSurface + INVASION_VERTICAL_TILES;
        }

        // The primary method we use to identify what NPC/projectile hurt the player, initialize combat instances for NPCs, and add fear points based on damage
        public override void OnHurt(Player.HurtInfo info)
        {
            base.OnHurt(info);

            if (info.Damage <= 0 || Player.statLife - info.Damage <= 0)
                return;

            int fearPoints = CalculateFearProgression(info.Damage, Player.statLifeMax2, Player.statLife);

            NPC sourceNPC = null;

            bool isProjectileDamage = info.DamageSource.SourceProjectileLocalIndex >= 0
                                      && info.DamageSource.SourceProjectileLocalIndex < Main.maxProjectiles;
            bool isDirectNPCDamage = info.DamageSource.SourceNPCIndex >= 0
                                      && info.DamageSource.SourceNPCIndex < Main.maxNPCs;

            if (isProjectileDamage)
            {
                Projectile proj = Main.projectile[info.DamageSource.SourceProjectileLocalIndex];
                if (proj.active)
                {
                    var projData = proj.GetGlobalProjectile<FearGlobalProjectile>();

                    if (projData.sourceNPCIndex == -2)
                        return;

                    if (projData.sourceNPCIndex >= 0)
                    {
                        NPC candidate = Main.npc[projData.sourceNPCIndex];
                        if (candidate.active)
                            sourceNPC = candidate;
                    }
                }
            }
            else if (isDirectNPCDamage)
            {
                NPC candidate = Main.npc[info.DamageSource.SourceNPCIndex];
                if (candidate.active)
                    sourceNPC = candidate;
            }

            if (sourceNPC != null)
            {
                CombatTracker.RecordEnemyDamage(sourceNPC, Player.whoAmI, info.Damage);

                if (PhobiaData.NPCPhobiaMap.TryGetValue(sourceNPC.type, out List<PhobiaID> npcPhobias))
                {
                    foreach (PhobiaID phobia in npcPhobias)
                    {
                        if (HasDebuff(phobia, PhobiaDebuff.PhobiaDebuffID.TraumaticStrike))
                        {
                            if (Main.rand.NextFloat() < 0.2f)
                                Player.AddBuff(ModContent.BuffType<TraumaticStrike>(), 30 * 60);
                            break;
                        }
                    }

                    foreach (PhobiaID phobia in npcPhobias)
                        AddFearPoints(phobia, fearPoints);
                }
            }

            CheckBiomePhobias(fearPoints);
            CheckEventPhobiasOnHurt(fearPoints);
            CheckBossPhobiasOnHurt(fearPoints);
            CheckDebuffPhobiasOnHurt(fearPoints);

            bool atSurface = Player.ZoneOverworldHeight || Player.ZoneSkyHeight;

            if (Player.wet && !Player.lavaWet && !Player.honeyWet)
                AddFearPoints(PhobiaID.Nerophobia, fearPoints);

            if (Player.lavaWet)
                AddFearPoints(PhobiaID.Ygrifotiaphobia, fearPoints);

            int tileX = (int)(Player.Center.X / 16f);
            int tileY = (int)(Player.Center.Y / 16f);
            Color lightColor = Lighting.GetColor(tileX, tileY);
            float brightness = (lightColor.R + lightColor.G + lightColor.B) / (255f * 3f);
            if (brightness < 0.15f)
                AddFearPoints(PhobiaID.Skotadiphobia, fearPoints);

            if (atSurface && Main.dayTime)
                AddFearPoints(PhobiaID.Imeraphobia, fearPoints);

            if (atSurface && !Main.dayTime)
                AddFearPoints(PhobiaID.Nychtaphobia, fearPoints);

            bonusMultiplier = 0f;
        }

        // Biome tracker for generic damage
        private void CheckBiomePhobias(int fearPoints)
        {
            if (Player.ZoneUnderworldHeight)
            {
                AddFearPoints(PhobiaID.Stygiophobia, fearPoints);
                return;
            }

            if (Player.ZoneDungeon)
                AddFearPoints(PhobiaID.Katakomvesphobia, fearPoints);

            if (Player.ZoneLihzhardTemple)
                AddFearPoints(PhobiaID.Archaioereipiophobia, fearPoints);

            if (Player.ZoneNormalCaverns)
                AddFearPoints(PhobiaID.Speluncaphobia, fearPoints);

            if (Player.ZoneCorrupt)
                AddFearPoints(PhobiaID.Kakigiphobia, fearPoints);

            if (Player.ZoneCrimson)
                AddFearPoints(PhobiaID.Hemophobia, fearPoints);

            if (Player.ZoneHallow)
                AddFearPoints(PhobiaID.Photophobia, fearPoints);

            if (Player.ZoneJungle)
                AddFearPoints(PhobiaID.Zounklaphobia, fearPoints);

            if (Player.ZoneSnow)
                AddFearPoints(PhobiaID.Chioniphobia, fearPoints);

            if (Player.ZoneBeach)
                AddFearPoints(PhobiaID.Thalassaphobia, fearPoints);

            if (Player.ZoneDesert || Player.ZoneUndergroundDesert)
                AddFearPoints(PhobiaID.Ammophobia, fearPoints);

            if (Player.ZoneGlowshroom)
                AddFearPoints(PhobiaID.Mycophobia, fearPoints);

            if (Player.ZoneForest)
                AddFearPoints(PhobiaID.Hylophobia, fearPoints);
        }

        private void CheckEventPhobiasOnHurt(int fearPoints)
        {
            foreach (PhobiaID phobia in GetActiveEventPhobias())
                AddFearPoints(phobia, fearPoints);
        }

        private List<PhobiaID> GetActiveEventPhobias()
        {
            var active = new List<PhobiaID>();

            bool atSurface = Player.ZoneOverworldHeight || Player.ZoneSkyHeight;
            bool atOverworld = Player.ZoneOverworldHeight;
            bool inInvasionRange = IsPlayerInInvasionRange();

            if (atSurface)
            {
                if (Main.bloodMoon) active.Add(PhobiaID.Kakofengariphobia);
                if (Main.raining) active.Add(PhobiaID.Ombrophobia);
                if (Main.eclipse) active.Add(PhobiaID.Kosmikophobia);
                if (Main.slimeRain) active.Add(PhobiaID.Gloiovrochiaphobia);
            }

            if (atOverworld && Player.ZoneSandstorm)
                active.Add(PhobiaID.Ammothyellaphobia);

            if (atOverworld)
            {
                if (Main.pumpkinMoon) active.Add(PhobiaID.Apokosmofengariphobia);
                if (Main.snowMoon) active.Add(PhobiaID.Pagomenofengariphobia);
            }

            if (Player.ZoneOldOneArmy)
                active.Add(PhobiaID.Archaiosstratosphobia);

            if (Player.ZoneTowerNebula || Player.ZoneTowerSolar
                || Player.ZoneTowerStardust || Player.ZoneTowerVortex)
                active.Add(PhobiaID.Seliniakieisvoliphobia);

            if (Main.invasionType != 0 && Main.invasionSize > 0 && inInvasionRange)
            {
                switch (Main.invasionType)
                {
                    case InvasionID.GoblinArmy:
                        active.Add(PhobiaID.Eisvolikalikantzaronphobia); break;
                    case InvasionID.SnowLegion:
                        active.Add(PhobiaID.Psychrosstratosphobia); break;
                    case InvasionID.PirateInvasion:
                        active.Add(PhobiaID.Peiratikiepithesiphobia); break;
                    case InvasionID.MartianMadness:
                        active.Add(PhobiaID.Exogiiniparafrosyniphobia); break;
                }
            }

            return active;
        }

        private void CheckBossPhobiasOnHurt(int fearPoints)
        {
            foreach (PhobiaID phobia in GetBossPhobiasInRange())
                AddFearPoints(phobia, fearPoints);
        }

        private HashSet<PhobiaID> GetBossPhobiasInRange()
        {
            Rectangle detectionBounds = new Rectangle(
                (int)Main.screenPosition.X,
                (int)Main.screenPosition.Y,
                Main.screenWidth,
                Main.screenHeight);
            detectionBounds.Inflate(5000, 5000);

            var result = new HashSet<PhobiaID>();

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];

                if (!npc.active)
                    continue;

                if (!npc.boss && NPCID.Sets.BossHeadTextures[npc.type] < 0)
                    continue;

                if (!npc.Hitbox.Intersects(detectionBounds))
                    continue;

                if (!PhobiaData.NPCPhobiaMap.TryGetValue(npc.type, out List<PhobiaID> phobias))
                    continue;

                foreach (PhobiaID phobia in phobias)
                {
                    if (PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition def)
                        && def.type == PhobiaDefinition.PhobiaType.Boss)
                    {
                        result.Add(phobia);
                    }
                }
            }

            return result;
        }

        private void CheckDebuffPhobiasOnHurt(int fearPoints)
        {
            foreach (PhobiaID phobia in GetActiveDebuffPhobias())
                AddFearPoints(phobia, fearPoints);
        }

        private HashSet<PhobiaID> GetActiveDebuffPhobias()
        {
            var result = new HashSet<PhobiaID>();

            foreach (var kvp in PhobiaData.DebuffPhobiaMap)
            {
                if (!Player.HasBuff(kvp.Key))
                    continue;

                foreach (PhobiaID phobia in kvp.Value)
                    result.Add(phobia);
            }

            return result;
        }

        // Similar concept to the 'OnHurt' method, but we clear combat data and apply a flat 33% fear progression for applicable phobias
        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        {
            base.Kill(damage, hitDirection, pvp, damageSource);

            CombatTracker.OnPlayerDeath(Player.whoAmI);

            if (damageSource.SourceNPCIndex >= 0 && damageSource.SourceNPCIndex < Main.maxNPCs)
            {
                NPC sourceNPC = Main.npc[damageSource.SourceNPCIndex];
                if (sourceNPC.active)
                {
                    ApplyNPCDeathPenalty(sourceNPC);
                    ApplyBiomeDeathPenalty();
                    ApplyEventDeathPenalty();
                    ApplyBossDeathPenalty();
                    ApplyEnvironmentalDeathPenalty();
                    ApplyDebuffDeathPenalty();
                    bonusMultiplier = 0f;
                    return;
                }
            }

            if (damageSource.SourceProjectileLocalIndex >= 0
                && damageSource.SourceProjectileLocalIndex < Main.maxProjectiles)
            {
                Projectile proj = Main.projectile[damageSource.SourceProjectileLocalIndex];
                if (proj.active)
                {
                    var projData = proj.GetGlobalProjectile<FearGlobalProjectile>();

                    if (projData.sourceNPCIndex == -2)
                        return;

                    if (projData.sourceNPCIndex >= 0)
                    {
                        NPC sourceNPC = Main.npc[projData.sourceNPCIndex];
                        if (sourceNPC.active)
                        {
                            ApplyNPCDeathPenalty(sourceNPC);
                            ApplyBiomeDeathPenalty();
                            ApplyEventDeathPenalty();
                            ApplyBossDeathPenalty();
                            ApplyEnvironmentalDeathPenalty();
                            ApplyDebuffDeathPenalty();
                            bonusMultiplier = 0f;
                            return;
                        }
                    }
                }

                ApplyBiomeDeathPenalty();
                ApplyEventDeathPenalty();
                ApplyBossDeathPenalty();
                ApplyEnvironmentalDeathPenalty();
                ApplyDebuffDeathPenalty();
                bonusMultiplier = 0f;
                return;
            }

            if (Player.wet && !Player.lavaWet && !Player.honeyWet)
                ApplyDeathPenalty(PhobiaID.Nerophobia);

            if (Player.lavaWet)
                ApplyDeathPenalty(PhobiaID.Ygrifotiaphobia);

            ApplyBiomeDeathPenalty();
            ApplyEventDeathPenalty();
            ApplyBossDeathPenalty();
            ApplyEnvironmentalDeathPenalty();
            ApplyDebuffDeathPenalty();
            bonusMultiplier = 0f;
        }

        private void ApplyDeathPenalty(PhobiaID phobia)
        {
            PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition definition);
            int penalty = playerPhobiaData[phobia].hasPhobia
                ? definition.postAcquisitionMax / 3
                : definition.preAcquisitionMax / 3;
            AddFearPoints(phobia, penalty);
        }

        private void ApplyNPCDeathPenalty(NPC sourceNPC)
        {
            if (!PhobiaData.NPCPhobiaMap.TryGetValue(sourceNPC.type, out List<PhobiaID> phobias))
                return;

            foreach (PhobiaID phobia in phobias)
                ApplyDeathPenalty(phobia);
        }

        // Helper method for applying death penalty to biome specific phobias
        private void ApplyBiomeDeathPenalty()
        {
            if (Player.ZoneUnderworldHeight)
            {
                ApplyDeathPenalty(PhobiaID.Stygiophobia);
                return;
            }

            if (Player.ZoneDungeon)
                ApplyDeathPenalty(PhobiaID.Katakomvesphobia);

            if (Player.ZoneLihzhardTemple)
                ApplyDeathPenalty(PhobiaID.Archaioereipiophobia);

            if (Player.ZoneNormalCaverns)
                ApplyDeathPenalty(PhobiaID.Speluncaphobia);

            if (Player.ZoneCorrupt)
                ApplyDeathPenalty(PhobiaID.Kakigiphobia);

            if (Player.ZoneCrimson)
                ApplyDeathPenalty(PhobiaID.Hemophobia);

            if (Player.ZoneHallow)
                ApplyDeathPenalty(PhobiaID.Photophobia);

            if (Player.ZoneJungle)
                ApplyDeathPenalty(PhobiaID.Zounklaphobia);

            if (Player.ZoneSnow)
                ApplyDeathPenalty(PhobiaID.Chioniphobia);

            if (Player.ZoneBeach)
                ApplyDeathPenalty(PhobiaID.Thalassaphobia);

            if (Player.ZoneDesert || Player.ZoneUndergroundDesert)
                ApplyDeathPenalty(PhobiaID.Ammophobia);

            if (Player.ZoneGlowshroom)
                ApplyDeathPenalty(PhobiaID.Mycophobia);

            if (Player.ZoneForest)
                ApplyDeathPenalty(PhobiaID.Hylophobia);
        }

        private void ApplyEventDeathPenalty()
        {
            foreach (PhobiaID phobia in GetActiveEventPhobias())
                ApplyDeathPenalty(phobia);
        }

        private void ApplyBossDeathPenalty()
        {
            foreach (PhobiaID phobia in GetBossPhobiasInRange())
                ApplyDeathPenalty(phobia);
        }

        private void ApplyEnvironmentalDeathPenalty()
        {
            bool atSurface = Player.ZoneOverworldHeight || Player.ZoneSkyHeight;

            int tileX = (int)(Player.Center.X / 16f);
            int tileY = (int)(Player.Center.Y / 16f);
            Color lightColor = Lighting.GetColor(tileX, tileY);
            float brightness = (lightColor.R + lightColor.G + lightColor.B) / (255f * 3f);

            if (brightness < 0.15f)
                ApplyDeathPenalty(PhobiaID.Skotadiphobia);

            if (atSurface && Main.dayTime)
                ApplyDeathPenalty(PhobiaID.Imeraphobia);

            if (atSurface && !Main.dayTime)
                ApplyDeathPenalty(PhobiaID.Nychtaphobia);
        }

        private void ApplyDebuffDeathPenalty()
        {
            foreach (PhobiaID phobia in GetActiveDebuffPhobias())
                ApplyDeathPenalty(phobia);
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
