using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

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
        private Dictionary<SetID, PlayerSetState> playerSetData = new Dictionary<SetID, PlayerSetState>();

        private HashSet<PhobiaID> unlockedPhobias = new HashSet<PhobiaID>();

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
            }

            foreach (SetID set in Enum.GetValues<SetID>())
            {
                playerSetData[set] = new PlayerSetState();
            }

            foreach (PhobiaID phobia in Enum.GetValues<PhobiaID>())
            {
                if (IsPhobiaUnlocked(phobia)) { }
            }
        }

        // Saves Player phobia data
        public override void SaveData(TagCompound tag)
        {
            var unlockedList = new List<string>();
            foreach (PhobiaID phobia in unlockedPhobias)
                unlockedList.Add(phobia.ToString());
            tag["unlockedPhobias"] = unlockedList;

            foreach (PhobiaID phobia in Enum.GetValues<PhobiaID>())
            {
                PlayerPhobiaState state = playerPhobiaData[phobia];

                if (!state.hasPhobia && state.fearPoints == 0 && state.couragePoints == 0)
                    continue;

                string key = phobia.ToString();
                tag[key + ".hasPhobia"] = state.hasPhobia;
                tag[key + ".isBurden"] = state.isBurden;
                tag[key + ".fearPoints"] = state.fearPoints;
                tag[key + ".couragePoints"] = state.couragePoints;
                tag[key + ".currentRank"] = state.currentRank;
            }
        }

        // Loads Player phobia data
        public override void LoadData(TagCompound tag)
        {
            unlockedPhobias.Clear();
            if (tag.ContainsKey("unlockedPhobias"))
            {
                var unlockedList = tag.GetList<string>("unlockedPhobias");
                foreach (string name in unlockedList)
                {
                    if (Enum.TryParse(name, true, out PhobiaID phobia))
                        unlockedPhobias.Add(phobia);
                }
            }

            foreach (PhobiaID phobia in Enum.GetValues<PhobiaID>())
            {
                string key = phobia.ToString();

                if (!tag.ContainsKey(key + ".hasPhobia"))
                    continue;

                PlayerPhobiaState state = playerPhobiaData[phobia];
                state.hasPhobia = tag.GetBool(key + ".hasPhobia");
                state.isBurden = tag.GetBool(key + ".isBurden");
                state.fearPoints = tag.GetInt(key + ".fearPoints");
                state.couragePoints = tag.GetInt(key + ".couragePoints");
                state.currentRank = tag.GetInt(key + ".currentRank");

                if (state.hasPhobia && state.currentRank > 1)
                {
                    for (int rank = 2; rank <= Math.Min(state.currentRank, 3); rank++)
                    {
                        PhobiaDebuff debuff = SelectDebuff(phobia, rank);
                        if (debuff != null)
                            state.activeDebuffs.Add(debuff);
                    }
                }

                if (state.isBurden && !state.activeDebuffs.Any(d => d.rank == 4))
                {
                    PhobiaDebuff debuff = SelectDebuff(phobia, 4);
                    if (debuff != null)
                        state.activeDebuffs.Add(debuff);
                }
            }

            foreach (SetID set in Enum.GetValues<SetID>())
                RecalculateSetRank(set);
        }

        // Method to determine if a phobia prereq is met or not
        public bool IsPhobiaUnlocked(PhobiaID phobia)
        {
            if (unlockedPhobias.Contains(phobia))
                return true;

            PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition def);

            if (def.prerequisite == null || def.prerequisite(Player))
            {
                unlockedPhobias.Add(phobia);
                return true;
            }

            return false;
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
            }

            var fearPool = new HashSet<PhobiaID>();

            if (sourceNPC != null
                && PhobiaData.NPCPhobiaMap.TryGetValue(sourceNPC.type, out List<PhobiaID> npcPhobias))
            {
                foreach (PhobiaID p in npcPhobias) fearPool.Add(p);
            }

            foreach (PhobiaID p in GetPlayerStatePhobias()) fearPool.Add(p);

            List<PhobiaID> filtered = FilterMaxedFear(FilterLockedPhobias(fearPool.ToList()));
            DistributeFearPoints(filtered, fearPoints);

            bonusMultiplier = 0f;
        }

        // Currently used to add damage amplifiers depending on the player phobia state
        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            float diffMult = 1f;
            if (Main.masterMode) diffMult = 2.67f;
            else if (Main.expertMode) diffMult = 1.45f;

            int totalPhobias = GetTotalPhobiaCount();

            int afflictionsRank = GetSetRank(SetID.Afflictions);
            if (afflictionsRank > 0)
            {
                bool hasAnyMappedDebuff = false;
                foreach (var kvp in PhobiaData.DebuffPhobiaMap)
                {
                    if (Player.HasBuff(kvp.Key))
                    {
                        hasAnyMappedDebuff = true;
                        break;
                    }
                }

                if (hasAnyMappedDebuff)
                {
                    float damageAmp = Math.Min(
                        afflictionsRank * 0.010f * diffMult * totalPhobias, 1.0f);
                    modifiers.IncomingDamageMultiplier *= (1f + damageAmp);
                }
            }

            int natureRank = GetSetRank(SetID.Nature);
            if (natureRank > 0)
            {
                bool atSurface = Player.ZoneOverworldHeight || Player.ZoneSkyHeight;
                if (atSurface)
                {
                    float damageAmp = Math.Min(
                        natureRank * 0.010f * diffMult * totalPhobias, 1.0f);
                    modifiers.IncomingDamageMultiplier *= (1f + damageAmp);
                }
            }

            int undergroundRank = GetSetRank(SetID.Underground);
            if (undergroundRank > 0)
            {
                int tileX = (int)(Player.Center.X / 16f);
                int tileY = (int)(Player.Center.Y / 16f);
                Color lightColor = Lighting.GetColor(tileX, tileY);
                float brightness = (lightColor.R + lightColor.G + lightColor.B) / (255f * 3f);

                if (brightness < 0.15f)
                {
                    float damageAmp = Math.Min(
                        undergroundRank * 0.010f * diffMult * totalPhobias, 1.0f);
                    modifiers.IncomingDamageMultiplier *= (1f + damageAmp);
                }
            }

            int oceanRank = GetSetRank(SetID.Ocean);
            if (oceanRank > 0 && Player.wet && !Player.lavaWet && !Player.honeyWet)
            {
                float damageAmp = Math.Min(
                    oceanRank * 0.010f * diffMult * totalPhobias, 1.0f);
                modifiers.IncomingDamageMultiplier *= (1f + damageAmp);
            }

            int hellRank = GetSetRank(SetID.Hell);
            if (hellRank > 0 && Player.lavaWet)
            {
                float damageAmp = Math.Min(
                    hellRank * 0.010f * diffMult * totalPhobias, 1.0f);
                modifiers.IncomingDamageMultiplier *= (1f + damageAmp);
            }
        }

        // Similar concept to the 'OnHurt' method, but we clear combat data and apply a flat 33% fear progression for applicable phobias
        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        {
            base.Kill(damage, hitDirection, pvp, damageSource);

            CombatTracker.OnPlayerDeath(Player.whoAmI);

            HashSet<PhobiaID> BuildDeathPool()
            {
                var pool = new HashSet<PhobiaID>();
                foreach (PhobiaID p in GetActiveBiomePhobias()) pool.Add(p);
                foreach (PhobiaID p in GetActiveEventPhobias(true)) pool.Add(p);
                foreach (PhobiaID p in GetBossPhobiasInRange()) pool.Add(p);
                foreach (PhobiaID p in GetActiveDebuffPhobias()) pool.Add(p);

                bool atSurface = Player.ZoneOverworldHeight || Player.ZoneSkyHeight;
                int tileX = (int)(Player.Center.X / 16f);
                int tileY = (int)(Player.Center.Y / 16f);
                Color lightColor = Lighting.GetColor(tileX, tileY);
                float brightness = (lightColor.R + lightColor.G + lightColor.B) / (255f * 3f);
                if (brightness < 0.15f) pool.Add(PhobiaID.Skotadiphobia);
                if (atSurface && Main.dayTime) pool.Add(PhobiaID.Imeraphobia);
                if (atSurface && !Main.dayTime) pool.Add(PhobiaID.Nychtaphobia);

                return pool;
            }

            if (damageSource.SourceNPCIndex >= 0 && damageSource.SourceNPCIndex < Main.maxNPCs)
            {
                NPC sourceNPC = Main.npc[damageSource.SourceNPCIndex];
                if (sourceNPC.active)
                {
                    var pool = BuildDeathPool();
                    if (PhobiaData.NPCPhobiaMap.TryGetValue(sourceNPC.type, out List<PhobiaID> npcPhobias))
                        foreach (PhobiaID p in npcPhobias) pool.Add(p);

                    var filtered = FilterMaxedFear(FilterLockedPhobias(pool.ToList()));
                    ApplyDeathPenalty(FilterMaxedFear(FilterLockedPhobias(pool.ToList())));
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
                            var pool = BuildDeathPool();
                            if (PhobiaData.NPCPhobiaMap.TryGetValue(sourceNPC.type, out List<PhobiaID> npcPhobias))
                                foreach (PhobiaID p in npcPhobias) pool.Add(p);

                            var filtered = FilterMaxedFear(FilterLockedPhobias(pool.ToList()));
                            ApplyDeathPenalty(FilterMaxedFear(FilterLockedPhobias(pool.ToList())));
                            bonusMultiplier = 0f;
                            return;
                        }
                    }
                }

                var generalPool = BuildDeathPool();
                ApplyDeathPenalty(FilterLockedPhobias(generalPool.ToList()));
                bonusMultiplier = 0f;
                return;
            }

            var envPool = BuildDeathPool();

            if (Player.wet && !Player.lavaWet && !Player.honeyWet)
                envPool.Add(PhobiaID.Nerophobia);

            if (Player.lavaWet)
                envPool.Add(PhobiaID.Ygrifotiaphobia);

            ApplyDeathPenalty(FilterMaxedFear(FilterLockedPhobias(envPool.ToList())));
            bonusMultiplier = 0f;
        }

        // Event tracker to see if the player is in an event's terror radius
        private void CheckEventTerrorRadius()
        {
            foreach (PhobiaID phobia in GetActiveEventPhobias())
            {
                if (phobia == PhobiaID.Ombrophobia || phobia == PhobiaID.Ammothyellaphobia)
                    continue;
                AddFearPoints(phobia, EVENT_BOSS_FEAR_PER_SECOND);
            }
        }

        // Boss tracker to see if a player is in it's terror radius
        private void CheckBossTerrorRadius()
        {
            foreach (PhobiaID phobia in GetBossPhobiasInRange())
                AddFearPoints(phobia, EVENT_BOSS_FEAR_PER_SECOND);
        }

        // Checks if the player is actively in an event zone near spawn
        private bool IsPlayerInInvasionRange()
        {
            int playerTileX = (int)(Player.Center.X / 16f);
            int playerTileY = (int)(Player.Center.Y / 16f);

            return Math.Abs(playerTileX - Main.spawnTileX) <= INVASION_HORIZONTAL_TILES
                   && playerTileY <= Main.worldSurface + INVASION_VERTICAL_TILES;
        }

        // Getter for all player state phobia getters
        public List<PhobiaID> GetPlayerStatePhobias()
        {
            var result = new HashSet<PhobiaID>();

            foreach (PhobiaID p in GetActiveBiomePhobias()) result.Add(p);
            foreach (PhobiaID p in GetActiveEventPhobias()) result.Add(p);
            foreach (PhobiaID p in GetBossPhobiasInRange()) result.Add(p);
            foreach (PhobiaID p in GetActiveDebuffPhobias()) result.Add(p);
            foreach (PhobiaID p in GetActiveEnvironmentalPhobias()) result.Add(p);

            return result.ToList();
        }

        // Getter for biomes the player is actively in
        private List<PhobiaID> GetActiveBiomePhobias()
        {
            var result = new List<PhobiaID>();

            if (Player.ZoneUnderworldHeight)
            {
                result.Add(PhobiaID.Stygiophobia);
                return result;
            }

            if (Player.ZoneDungeon) result.Add(PhobiaID.Katakomvesphobia);
            if (Player.ZoneLihzhardTemple) result.Add(PhobiaID.Archaioereipiophobia);
            if (Player.ZoneNormalCaverns) result.Add(PhobiaID.Speluncaphobia);
            if (Player.ZoneCorrupt) result.Add(PhobiaID.Kakigiphobia);
            if (Player.ZoneCrimson) result.Add(PhobiaID.Hemophobia);
            if (Player.ZoneHallow) result.Add(PhobiaID.Photophobia);
            if (Player.ZoneJungle) result.Add(PhobiaID.Zounklaphobia);
            if (Player.ZoneSnow) result.Add(PhobiaID.Chioniphobia);
            if (Player.ZoneBeach) result.Add(PhobiaID.Thalassaphobia);
            if (Player.ZoneDesert || Player.ZoneUndergroundDesert)
                result.Add(PhobiaID.Ammophobia);
            if (Player.ZoneGlowshroom) result.Add(PhobiaID.Mycophobia);
            if (Player.ZoneForest) result.Add(PhobiaID.Hylophobia);

            return result;
        }

        // Getter for events the player is actively in
        private List<PhobiaID> GetActiveEventPhobias(bool debugPrint = false)
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

            if (atOverworld && Player.ZoneSandstorm && Player.ZoneDesert)
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

        // Getter for bosses the player is actively near
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

        // Getter for the player's active debuffs
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

        // Getter for certain player enviromental states
        private List<PhobiaID> GetActiveEnvironmentalPhobias()
        {
            var result = new List<PhobiaID>();

            bool atSurface = Player.ZoneOverworldHeight || Player.ZoneSkyHeight;

            if (Player.wet && !Player.lavaWet && !Player.honeyWet)
                result.Add(PhobiaID.Nerophobia);

            if (Player.lavaWet)
                result.Add(PhobiaID.Ygrifotiaphobia);

            int tileX = (int)(Player.Center.X / 16f);
            int tileY = (int)(Player.Center.Y / 16f);
            Color lightColor = Lighting.GetColor(tileX, tileY);
            float brightness = (lightColor.R + lightColor.G + lightColor.B) / (255f * 3f);
            if (brightness < 0.15f)
                result.Add(PhobiaID.Skotadiphobia);

            if (atSurface && Main.dayTime)
                result.Add(PhobiaID.Imeraphobia);

            if (atSurface && !Main.dayTime)
                result.Add(PhobiaID.Nychtaphobia);

            return result;
        }

        public List<PhobiaID> FilterLockedPhobias(List<PhobiaID> phobias)
        {
            phobias.RemoveAll(p => !IsPhobiaUnlocked(p));
            return phobias;
        }

        // Helper method that produces fear points from health damage
        private int CalculateFearProgression(int damage, int maxHP, int currentHP)
        {
            float normalizedDamage = damage * (REFERENCE_HP / (float)maxHP);
            float healthModifier = 1.0f - ((float)currentHP / maxHP) * 0.5f;
            float fearPoints = normalizedDamage * healthModifier;

            return (int)Math.Floor(fearPoints);
        }

        // Filters out any phobias that already have max Fear points
        private List<PhobiaID> FilterMaxedFear(IEnumerable<PhobiaID> phobias)
        {
            var result = new List<PhobiaID>();

            foreach (PhobiaID phobia in phobias)
            {
                PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition def);
                int cap = playerPhobiaData[phobia].hasPhobia
                    ? def.postAcquisitionMax
                    : def.preAcquisitionMax;

                if (playerPhobiaData[phobia].fearPoints < cap)
                    result.Add(phobia);
            }

            return result;
        }

        // Distributes the Fear among all applicable phobias at any given moment
        private void DistributeFearPoints(List<PhobiaID> phobias, int totalPoints)
        {
            if (phobias.Count == 0 || totalPoints <= 0)
                return;

            if (totalPoints <= phobias.Count)
            {
                for (int i = phobias.Count - 1; i > 0; i--)
                {
                    int j = Main.rand.Next(i + 1);
                    (phobias[i], phobias[j]) = (phobias[j], phobias[i]);
                }
                for (int i = 0; i < totalPoints; i++)
                {
                    AddFearPoints(phobias[i], 1);
                }
                return;
            }

            List<PhobiaID> remaining = new List<PhobiaID>(phobias);
            int remainingPoints = totalPoints;

            while (remaining.Count > 1)
            {
                int idx = Main.rand.Next(remaining.Count);
                PhobiaID phobia = remaining[idx];
                remaining.RemoveAt(idx);

                int maxAssignable = remainingPoints - remaining.Count;
                int assigned = Main.rand.Next(1, maxAssignable + 1);

                PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition def);
                int capacity = def.postAcquisitionMax - playerPhobiaData[phobia].fearPoints;

                if (assigned > capacity)
                {
                    AddFearPoints(phobia, capacity);
                    remainingPoints -= capacity;
                    remainingPoints += (assigned - capacity);
                }
                else
                {
                    AddFearPoints(phobia, assigned);
                    remainingPoints -= assigned;
                }
            }
            AddFearPoints(remaining[0], remainingPoints);
        }

        // Contains all logic surrounding adding fear points to any phobia in the mod
        public void AddFearPoints(PhobiaID phobia, int points)
        {
            PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition definition);

            if (playerPhobiaData[phobia].isBurden)
            {
                return;
            }

            if (!IsPhobiaUnlocked(phobia))
                return;

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
                    RecalculateSetRank(PhobiaData.Definitions[phobia].set);
                }
            }
        }

        // Filters out phobias that are already at max courage
        public List<PhobiaID> FilterMaxedCourage(IEnumerable<PhobiaID> phobias)
        {
            var result = new List<PhobiaID>();

            foreach (PhobiaID phobia in phobias)
            {
                var state = playerPhobiaData[phobia];

                if (!state.hasPhobia)
                {
                    if (state.fearPoints > 0)
                        result.Add(phobia);
                }
                else
                {
                    PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition def);
                    bool fullyMaxed = state.fearPoints == 0 && state.couragePoints >= def.courageMax;
                    if (!fullyMaxed)
                        result.Add(phobia);
                }
            }

            return result;
        }

        // Distributes courage to applicable phobias
        public void DistributeCouragePoints(List<PhobiaID> phobias, int totalPoints)
        {
            if (phobias.Count == 0 || totalPoints <= 0)
                return;

            if (totalPoints <= phobias.Count)
            {
                for (int i = phobias.Count - 1; i > 0; i--)
                {
                    int j = Main.rand.Next(i + 1);
                    (phobias[i], phobias[j]) = (phobias[j], phobias[i]);
                }
                for (int i = 0; i < totalPoints; i++)
                {
                    AddCouragePoints(phobias[i], 1);
                }
                return;
            }

            List<PhobiaID> remaining = new List<PhobiaID>(phobias);
            int remainingPoints = totalPoints;

            while (remaining.Count > 1)
            {
                int idx = Main.rand.Next(remaining.Count);
                PhobiaID phobia = remaining[idx];
                remaining.RemoveAt(idx);

                int maxAssignable = remainingPoints - remaining.Count;
                int assigned = Main.rand.Next(1, maxAssignable + 1);

                int capacity;
                if (!playerPhobiaData[phobia].hasPhobia)
                    capacity = playerPhobiaData[phobia].fearPoints;
                else
                {
                    PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition def);
                    capacity = playerPhobiaData[phobia].fearPoints
                               + (def.courageMax - playerPhobiaData[phobia].couragePoints);
                }

                if (assigned > capacity)
                {
                    AddCouragePoints(phobia, capacity);
                    remainingPoints -= capacity;
                    remainingPoints += (assigned - capacity);
                }
                else
                {
                    AddCouragePoints(phobia, assigned);
                    remainingPoints -= assigned;
                }
            }
            AddCouragePoints(remaining[0], remainingPoints);
        }

        // Contains all logic surrounding adding courage points to any phobia in the mod
        public void AddCouragePoints(PhobiaID phobia, int points)
        {
            if (playerPhobiaData[phobia].isBurden)
            {
                return;
            }

            if (!IsPhobiaUnlocked(phobia))
                return;

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

                if (playerPhobiaData[phobia].couragePoints >= definition.courageMax)
                {
                    ConquerPhobia(phobia);
                    return;
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

                    if (playerPhobiaData[phobia].couragePoints >= definition.courageMax)
                    {
                        ConquerPhobia(phobia);
                        return;
                    }
                }
            }

            int calculatedRank = CalculateRank(definition, playerPhobiaData[phobia].fearPoints, playerPhobiaData[phobia].currentRank, false);
            HandlePhobiaRank(phobia, calculatedRank);
        }

        // Removes phobias once the max courage is met
        private void ConquerPhobia(PhobiaID phobia)
        {
            SetID set = PhobiaData.Definitions[phobia].set;

            while (playerPhobiaData[phobia].activeDebuffs.Count > 0)
            {
                RemovePhobiaDebuff(phobia, playerPhobiaData[phobia].currentRank);
                if (playerPhobiaData[phobia].currentRank > 1)
                    playerPhobiaData[phobia].currentRank--;
                else
                    break;
            }

            playerPhobiaData[phobia].fearPoints = 0;
            playerPhobiaData[phobia].couragePoints = 0;
            playerPhobiaData[phobia].hasPhobia = false;
            playerPhobiaData[phobia].isBurden = false;
            playerPhobiaData[phobia].currentRank = 1;
            playerPhobiaData[phobia].activeDebuffs.Clear();
            unlockedPhobias.Remove(phobia);

            RecalculateSetRank(set);

            Main.NewText($"{phobia} has been conquered!", Color.Green);
        }

        // Helper method that removes fear from phobias
        public void RemoveFearPoints(PhobiaID phobia, int points)
        {
            playerPhobiaData[phobia].fearPoints -= points;
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
                    RecalculateSetRank(PhobiaData.Definitions[phobia].set);

                    if (playerPhobiaData[phobia].fearPoints > definition.postAcquisitionMax)
                    {
                        playerPhobiaData[phobia].fearPoints = definition.postAcquisitionMax;
                    }

                    int calculatedRank = CalculateRank(definition, playerPhobiaData[phobia].fearPoints, playerPhobiaData[phobia].currentRank, increasing);
                    HandlePhobiaRank(phobia, calculatedRank);
                }
            }
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

        // Applies a death penalty to applicable phobias
        private void ApplyDeathPenalty(List<PhobiaID> phobias)
        {
            if (phobias.Count == 0)
                return;

            int total = phobias.Count;

            for (int i = phobias.Count - 1; i > 0; i--)
            {
                int j = Main.rand.Next(i + 1);
                (phobias[i], phobias[j]) = (phobias[j], phobias[i]);
            }

            for (int i = 0; i < phobias.Count; i++)
            {
                PhobiaID phobia = phobias[i];
                int p = phobias.Count - i;

                PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition def);
                int gaugeSize = playerPhobiaData[phobia].hasPhobia
                    ? def.postAcquisitionMax
                    : def.preAcquisitionMax;

                float fraction = (1f / 3f) * ((float)p / total);
                int penalty = Math.Max(1, (int)(fraction * gaugeSize));

                AddFearPoints(phobia, penalty);
            }
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
            if (debuff == null) return;
            playerPhobiaData[phobia].activeDebuffs.Add(debuff);
        }

        // This method randomly selects a phobia debuff depending on the phobia and rank
        public static PhobiaDebuff SelectDebuff(PhobiaID phobia, int rank)
        {
            PhobiaData.Definitions.TryGetValue(phobia, out PhobiaDefinition definition);
            PhobiaDefinition.PhobiaType phobiaType = definition.type;

            List<PhobiaDebuff> typeList = PhobiaDebuffData.typeDebuffs.ContainsKey(phobiaType)
                ? PhobiaDebuffData.typeDebuffs[phobiaType]
                : new List<PhobiaDebuff>();

            List<PhobiaDebuff> phobiaSpecificList = PhobiaDebuffData.phobiaSpecificDebuffs.ContainsKey(phobia)
                ? PhobiaDebuffData.phobiaSpecificDebuffs[phobia]
                : new List<PhobiaDebuff>();

            List<PhobiaDebuff> typeFiltered = typeList.Where(d => d.rank == rank).ToList();
            List<PhobiaDebuff> phobiaSpecificFiltered = phobiaSpecificList.Where(d => d.rank == rank).ToList();

            List<PhobiaDebuff> combinedList = typeFiltered.Concat(phobiaSpecificFiltered).ToList();

            if (combinedList.Count == 0) return null;

            return combinedList[Random.Shared.Next(0, combinedList.Count)];
        }

        // Helper method that remove a debuff depending on phobia rank
        private void RemovePhobiaDebuff(PhobiaID phobia, int rank)
        {
            PhobiaDebuff debuff = playerPhobiaData[phobia].activeDebuffs.FirstOrDefault(activeDebuff => activeDebuff.rank == rank);
            playerPhobiaData[phobia].activeDebuffs.Remove(debuff);
        }

        // Calculates the rank of a set when a phobia is obtained
        public void RecalculateSetRank(SetID setID)
        {
            int count = 0;
            foreach (var kvp in PhobiaData.Definitions)
            {
                if (kvp.Value.set == setID && playerPhobiaData[kvp.Key].hasPhobia)
                    count++;
            }

            PhobiaSetData.Definitions.TryGetValue(setID, out PhobiaSet def);

            int newRank = 0;
            if (count >= def.rank3Threshold) newRank = 3;
            else if (count >= def.rank2Threshold) newRank = 2;
            else if (count >= def.rank1Threshold) newRank = 1;

            playerSetData[setID].currentRank = newRank;
        }

        // Returns the current rank of a set
        public int GetSetRank(SetID setID)
        {
            return playerSetData[setID].currentRank;
        }

        // Returns how many phobias the player currently has in a given set
        public int GetSetPhobiaCount(SetID setID)
        {
            int count = 0;
            foreach (var kvp in PhobiaData.Definitions)
            {
                if (kvp.Value.set == setID && playerPhobiaData[kvp.Key].hasPhobia)
                    count++;
            }
            return count;
        }

        // Returns how many phobias the player has in total
        public int GetTotalPhobiaCount()
        {
            int count = 0;
            foreach (PhobiaID phobia in Enum.GetValues<PhobiaID>())
            {
                if (playerPhobiaData[phobia].hasPhobia)
                    count++;
            }
            return count;
        }
    }
}
