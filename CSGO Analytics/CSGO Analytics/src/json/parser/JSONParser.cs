using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DemoInfoModded;
using Newtonsoft.Json;
using CSGO_Analytics.src.json.jsonobjects;
using CSGO_Analytics.src.data.gameevents;
using CSGO_Analytics.src.data.gameobjects;
    
namespace CSGO_Analytics.src.json.parser
{
    class JSONParser
    {

        private static StreamWriter outputStream;


        public JSONParser(string path)
        {
            string outputpath = path.Replace(".dem", "") + ".json";
            outputStream = new StreamWriter(outputpath);
        }

        /// <summary>
        /// Dumps the Gamestate in prettyjson or as one-liner(default)
        /// </summary>
        /// <param name="gs"></param>
        /// <param name="prettyjson"></param>
        public void dumpJSONFile(JSONGamestate gs, bool prettyjson)
        {
            Formatting f = Formatting.None;
            if (prettyjson)
                f = Formatting.Indented;

            outputStream.Write(JsonConvert.SerializeObject(gs, f));
        }

        /// <summary>
        /// Dumps gamestate in a string
        /// </summary>
        /// <param name="gs"></param>
        /// <param name="prettyjson"></param>
        public string dumpJSONString(JSONGamestate gs, bool prettyjson)
        {
            Formatting f = Formatting.None;
            if (prettyjson)
                f = Formatting.Indented;
            return JsonConvert.SerializeObject(gs, f);
        }

        public void stopParser()
        {
            outputStream.Close();
        }

        

        public JSONGamemeta assembleGamemeta(string mapname, float tickrate, IEnumerable<DemoInfoModded.Player> players)
        {
            return new JSONGamemeta
            {
                gamestate_id = 0,
                mapname = mapname,
                tickrate = tickrate,
                players = assemblePlayers(players)
            };
        }
        #region Gameevents

        public PlayerKilled assemblePlayerKilled(PlayerKilledEventArgs pke)
        {
            return new PlayerKilled
            {
                gameevent = "player_killed",
                actor = assemblePlayerDetailed(pke.Killer),
                victim = assemblePlayerDetailed(pke.Victim),
                headshot = pke.Headshot,
                penetrated = pke.PenetratedObjects,
                weapon = assembleWeapon(pke.Weapon)
            };
        }

        public WeaponFire assembleWeaponFire(WeaponFiredEventArgs we)
        {
            return new WeaponFire
            {
                gameevent = "weapon_fire",
                actor = assemblePlayerDetailed(we.Shooter),
                weapon = assembleWeapon(we.Weapon)
            };
        }

        public MovementEvents assemblePlayerSpotted(PlayerSpottedEventArgs e)
        {
            return new MovementEvents
            {
                gameevent = "player_spotted",
                actor = assemblePlayerDetailed(e.player),
            };
        }

        public PlayerHurt assemblePlayerHurt(PlayerHurtEventArgs phe)
        {
            PlayerHurt ph = new PlayerHurt
            {
                gameevent = "player_hurt",
                actor = assemblePlayerDetailed(phe.Attacker),
                victim = assemblePlayerDetailed(phe.Player),
                armor = phe.Armor,
                armor_damage = phe.ArmorDamage,
                HP = phe.Health,
                HP_damage = phe.HealthDamage,
                hitgroup = (int)phe.Hitgroup,
                weapon = assembleWeapon(phe.Weapon)
            };
            return ph;
        }

        public MovementEvents assemblePlayerPosition(DemoInfoModded.Player p)
        {
            return new MovementEvents
            {
                gameevent = "player_position",
                actor = assemblePlayerDetailed(p)
            };
        }

        #region Nades

        public NadeEvents assembleNade(NadeEventArgs e, string eventname)
        {
            DemoInfoModded.Player[] ps = null;

            if (e.GetType() == typeof(FlashEventArgs)) //Exception for FlashEvents -> we need flashed players
            {
                FlashEventArgs f = e as FlashEventArgs;
                ps = f.FlashedPlayers;
                return new FlashNade
                {
                    gameevent = eventname,
                    actor = assemblePlayerDetailed(e.ThrownBy),
                    nadetype = e.NadeType.ToString(),
                    position = new CSGO_Analytics.src.math.Vector { x = e.Position.X, y = e.Position.Y, z = e.Position.Z },
                    flashedplayers = assembleFlashedPlayers(f.FlashedPlayers)
                };
            }

            return new NadeEvents
            {
                gameevent = eventname,
                actor = assemblePlayerDetailed(e.ThrownBy),
                nadetype = e.NadeType.ToString(),
                position = new CSGO_Analytics.src.math.Vector { x = e.Position.X, y = e.Position.Y, z = e.Position.Z },
            };
        }


        #endregion

        #endregion



        #region SUBEVENTS

        public List<CSGO_Analytics.src.data.gameobjects.Player> assemblePlayers(DemoInfoModded.Player[] ps)
        {
            if (ps == null)
                return null;
            List<CSGO_Analytics.src.data.gameobjects.Player> players = new List<CSGO_Analytics.src.data.gameobjects.Player>();
            foreach (var player in ps)
                players.Add(assemblePlayer(player));

            return players;
        }

        public List<PlayerFlashed> assembleFlashedPlayers(DemoInfoModded.Player[] ps)
        {
            if (ps == null)
                return null;
            List<PlayerFlashed> players = new List<PlayerFlashed>();
            foreach (var player in ps)
                players.Add(assembleFlashPlayer(player));

            return players;
        }

        private PlayerFlashed assembleFlashPlayer(DemoInfoModded.Player p)
        {
            PlayerFlashed player = new PlayerFlashed
            {
                playername = p.Name,
                player_id = p.EntityID,
                position = new CSGO_Analytics.src.math.Vector { x = p.Position.X, y = p.Position.Y, z = p.Position.Z },
                facing = new Facing { yaw = p.ViewDirectionY, pitch = p.ViewDirectionX },
                team = p.Team.ToString(),
                flashedduration = p.FlashDuration
            };
            return player;
        }

        internal Gameevent assemblePlayerJumped(PlayerJumpedEventArgs e)
        {
            return new MovementEvents
            {
                gameevent = "player_jumped",
                actor = assemblePlayerDetailed(e.Jumper)
            };
        }

        internal Gameevent assemblePlayerFallen(PlayerFallEventArgs e)
        {
            return new MovementEvents
            {
                gameevent = "player_fallen",
                actor = assemblePlayerDetailed(e.Fallen)
            };
        }

        internal Gameevent assembleWeaponReload(WeaponReloadEventArgs we)
        {
            return new MovementEvents
            {
                gameevent = "weapon_reload",
                actor = assemblePlayerDetailed(we.Actor)
            };
        }

        internal Gameevent assemblePlayerStepped(PlayerSteppedEventArgs e)
        {
            return new MovementEvents
            {
                gameevent = "player_footstep",
                actor = assemblePlayerDetailed(e.Stepper)
            };
        }

        public List<PlayerMeta> assemblePlayers(IEnumerable<DemoInfoModded.Player> ps)
        {
            if (ps == null)
                return null;
            List<PlayerMeta> players = new List<PlayerMeta>();
            foreach (var player in ps)
                players.Add(assemblePlayerMeta(player));

            return players;
        }


        public CSGO_Analytics.src.data.gameobjects.Player assemblePlayer(DemoInfoModded.Player p)
        {
            CSGO_Analytics.src.data.gameobjects.Player player = new CSGO_Analytics.src.data.gameobjects.Player
            {
                playername = p.Name,
                player_id = p.EntityID,
                position = new CSGO_Analytics.src.math.Vector { x = p.Position.X, y = p.Position.Y, z = p.Position.Z },
                facing = new Facing { yaw = p.ViewDirectionY, pitch = p.ViewDirectionX },
                team = p.Team.ToString(),
            };
            return player;
        }

        public PlayerMeta assemblePlayerMeta(DemoInfoModded.Player p)
        {
            PlayerMeta player = new PlayerMeta
            {
                playername = p.Name,
                player_id = p.EntityID,
                team = p.Team.ToString(),
                clanname = p.AdditionaInformations.Clantag,
                steam_id = p.SteamID,
            };
            return player;
        }

        public PlayerDetailed assemblePlayerDetailed(DemoInfoModded.Player p)
        {
            PlayerDetailed playerdetailed = new PlayerDetailed
            {
                playername = p.Name,
                player_id = p.EntityID,
                position = new CSGO_Analytics.src.math.Vector { x = p.Position.X, y = p.Position.Y, z = p.Position.Z },
                facing = new Facing { yaw = p.ViewDirectionY, pitch = p.ViewDirectionX },
                team = p.Team.ToString(),
                isDucking = p.IsDucking,
                isSpotted = p.IsSpotted,
                isScoped = p.IsScoped,
                isWalking = p.IsWalking,
                hasHelmet = p.HasHelmet,
                hasDefuser = p.HasDefuseKit,
                HP = p.HP,
                armor = p.Armor,
                velocity = p.Velocity.Absolute //Length of Movementvector -> Velocity
            };

            return playerdetailed;
        }


        public PlayerDetailedWithItems assemblePlayerDetailedWithItems(DemoInfoModded.Player p)
        {
            PlayerDetailedWithItems playerdetailed = new PlayerDetailedWithItems
            {
                playername = p.Name,
                player_id = p.EntityID,
                position = new CSGO_Analytics.src.math.Vector { x = p.Position.X, y = p.Position.Y, z = p.Position.Z },
                facing = new Facing { yaw = p.ViewDirectionY, pitch = p.ViewDirectionX },
                team = p.Team.ToString(),
                isDucking = p.IsDucking,
                hasHelmet = p.HasHelmet,
                hasDefuser = p.HasDefuseKit,
                HP = p.HP,
                armor = p.Armor,
                velocity = p.Velocity.Absolute, //Length of Movementvector -> Velocity
                items = assembleWeapons(p.Weapons)
            };

            return playerdetailed;
        }

        public List<Weapon> assembleWeapons(IEnumerable<Equipment> wps)
        {
            List<Weapon> jwps = new List<Weapon>();
            foreach (var w in wps)
                jwps.Add(assembleWeapon(w));

            return jwps;
        }

        public Weapon assembleWeapon(Equipment wp)
        {
            if (wp == null)
            {
                Console.WriteLine("Weapon null. Bytestream not suitable for this version of DemoInfo");
                return new Weapon();
            }

            Weapon jwp = new Weapon
            {
                name = wp.Weapon.ToString(),
                ammo_in_magazine = wp.AmmoInMagazine
            };

            return jwp;
        }

        #endregion

        #region Bombevents

        public BombEvents assembleBomb(BombEventArgs be, string gameevent)
        {
            return new BombEvents
            {
                gameevent = gameevent,
                site = be.Site,
                actor = assemblePlayerDetailed(be.Player)
            };
        }

        public BombState assembleBombState(BombDropEventArgs be, string gameevent)
        {
            return new BombState
            {
                gameevent = gameevent,
                actor = assemblePlayerDetailed(be.Player)
            };
        }

        public BombState assembleBombState(BombPickUpEventArgs be, string gameevent)
        {
            return new BombState
            {
                gameevent = gameevent,
                actor = assemblePlayerDetailed(be.Player)
            };
        }

        public BombEvents assembleBombDefuse(BombDefuseEventArgs bde, string gameevent)
        {
            return new BombEvents
            {
                gameevent = gameevent,
                site = bde.Site,
                actor = assemblePlayerDetailed(bde.Player),
                haskit = bde.HasKit
            };
        }
        #endregion

        public WeaponFire assembleWeaponFireEmpty(WeaponFiredEmptyEventArgs we)
        {
            return new WeaponFire
            {
                gameevent = "weapon_fire_empty",
                actor = assemblePlayerDetailed(we.Shooter),
                weapon = assembleWeapon(we.Weapon)
            };
        }
    }


}