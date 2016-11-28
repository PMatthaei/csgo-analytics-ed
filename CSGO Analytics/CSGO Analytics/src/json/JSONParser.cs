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
using CS = CSGO_Analytics.src.data.gameobjects;
    
namespace CSGO_Analytics.src.json.parser
{
    class JSONParser
    {

        private static StreamWriter outputStream;

        private JsonSerializerSettings settings;

        public JSONParser(string path, JsonSerializerSettings settings)
        {
            string outputpath = path.Replace(".dem", "") + ".json";
            outputStream = new StreamWriter(outputpath);

            this.settings = settings;

        }

        /// <summary>
        /// Dumps the Gamestate in prettyjson or as one-liner(default)
        /// </summary>
        /// <param name="gs"></param>
        /// <param name="prettyjson"></param>
        public void dumpJSONFile(Gamestate gs, bool prettyjson)
        {
            Formatting f = Formatting.None;
            if (prettyjson)
                f = Formatting.Indented;

            outputStream.Write(JsonConvert.SerializeObject(gs, settings));
        }

        public Gamestate deserializeGamestate(string gamestatestring)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Gamestate>(gamestatestring, settings);
        }

        /// <summary>
        /// Dumps gamestate in a string
        /// </summary>
        /// <param name="gs"></param>
        /// <param name="prettyjson"></param>
        public string dumpJSONString(Gamestate gs, bool prettyjson)
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

        

        public GamestateMeta assembleGamemeta(string mapname, float tickrate, IEnumerable<DemoInfoModded.Player> players)
        {
            return new GamestateMeta
            {
                gamestate_id = 0,
                mapname = mapname,
                tickrate = tickrate,
                players = assemblePlayers(players.ToArray())
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
                assister = assemblePlayerDetailed(pke.Assister),
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

        public WeaponFire assembleWeaponFireEmpty(WeaponFiredEmptyEventArgs we)
        {
            return new WeaponFire
            {
                gameevent = "weapon_fire_empty",
                actor = assemblePlayerDetailed(we.Shooter),
                weapon = assembleWeapon(we.Weapon)
            };
        }

        public PlayerSpotted assemblePlayerSpotted(PlayerSpottedEventArgs e)
        {
            return new PlayerSpotted
            {
                gameevent = "player_spotted",
                actor = assemblePlayerDetailed(e.player)
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

        #region Subevents

        public List<CSGO_Analytics.src.data.gameobjects.Player> assemblePlayers(DemoInfoModded.Player[] ps)
        {
            if (ps == null)
                return null;
            List<CSGO_Analytics.src.data.gameobjects.Player> players = new List<CSGO_Analytics.src.data.gameobjects.Player>();
            foreach (var player in ps)
                players.Add(assemblePlayer(player));

            return players;
        }

        public List<CS.PlayerFlashed> assembleFlashedPlayers(DemoInfoModded.Player[] ps)
        {
            if (ps == null)
                return null;
            List<CS.PlayerFlashed> players = new List<CS.PlayerFlashed>();
            foreach (var player in ps)
                players.Add(assembleFlashPlayer(player));

            return players;
        }

        private CS.PlayerFlashed assembleFlashPlayer(DemoInfoModded.Player p)
        {
            CS.PlayerFlashed player = new CS.PlayerFlashed
            {
                playername = p.Name,
                player_id = p.EntityID,
                position = new CSGO_Analytics.src.math.Vector { x = p.Position.X, y = p.Position.Y, z = p.Position.Z },
                facing = new CS.Facing { yaw = p.ViewDirectionX, pitch = p.ViewDirectionY },
                team = p.Team.ToString(),
                flashedduration = p.FlashDuration
                
            };
            return player;
        }

        internal Event assemblePlayerJumped(PlayerJumpedEventArgs e)
        {
            return new MovementEvents
            {
                gameevent = "player_jumped",
                actor = assemblePlayerDetailed(e.Jumper)
            };
        }

        internal Event assemblePlayerFallen(PlayerFallEventArgs e)
        {
            return new MovementEvents
            {
                gameevent = "player_fallen",
                actor = assemblePlayerDetailed(e.Fallen)
            };
        }

        internal Event assembleWeaponReload(WeaponReloadEventArgs we)
        {
            return new MovementEvents
            {
                gameevent = "weapon_reload",
                actor = assemblePlayerDetailed(we.Actor)
            };
        }

        internal Event assemblePlayerStepped(PlayerSteppedEventArgs e)
        {
            return new MovementEvents
            {
                gameevent = "player_footstep",
                actor = assemblePlayerDetailed(e.Stepper)
            };
        }

        /*public List<PlayerMeta> assemblePlayers(IEnumerable<DemoInfoModded.Player> ps)
        {
            if (ps == null)
                return null;
            List<PlayerMeta> players = new List<PlayerMeta>();
            foreach (var player in ps)
                players.Add(assemblePlayerMeta(player));

            return players;
        }*/


        public CSGO_Analytics.src.data.gameobjects.Player assemblePlayer(DemoInfoModded.Player p)
        {
            return new CSGO_Analytics.src.data.gameobjects.Player
            {
                playername = p.Name,
                player_id = p.EntityID,
                position = new CSGO_Analytics.src.math.Vector { x = p.Position.X, y = p.Position.Y, z = p.Position.Z },
                facing = new CS.Facing { yaw = p.ViewDirectionX, pitch = p.ViewDirectionY },
                team = p.Team.ToString(),
                isSpotted = p.IsSpotted
            };
        }

        public CS.PlayerMeta assemblePlayerMeta(DemoInfoModded.Player p)
        {
            return new CS.PlayerMeta
            {
                playername = p.Name,
                player_id = p.EntityID,
                team = p.Team.ToString(),
                clanname = p.AdditionaInformations.Clantag,
                steam_id = p.SteamID,
            };
        }

        public CS.PlayerDetailed assemblePlayerDetailed(DemoInfoModded.Player p)
        {
            if (p == null) return null;

            return new CS.PlayerDetailed
            {
                playername = p.Name,
                player_id = p.EntityID,
                position = new CSGO_Analytics.src.math.Vector { x = p.Position.X, y = p.Position.Y, z = p.Position.Z },
                facing = new CS.Facing { yaw = p.ViewDirectionX, pitch = p.ViewDirectionY },
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
        }


        public CS.PlayerDetailedWithItems assemblePlayerDetailedWithItems(DemoInfoModded.Player p)
        {
            CS.PlayerDetailedWithItems playerdetailed = new CS.PlayerDetailedWithItems
            {
                playername = p.Name,
                player_id = p.EntityID,
                position = new CSGO_Analytics.src.math.Vector { x = p.Position.X, y = p.Position.Y, z = p.Position.Z },
                facing = new CS.Facing { yaw = p.ViewDirectionX, pitch = p.ViewDirectionY },
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

        public List<CS.Weapon> assembleWeapons(IEnumerable<Equipment> wps)
        {
            List<CS.Weapon> jwps = new List<CS.Weapon>();
            foreach (var w in wps)
                jwps.Add(assembleWeapon(w));

            return jwps;
        }

        public CS.Weapon assembleWeapon(Equipment wp)
        {
            if (wp == null)
            {
                Console.WriteLine("Weapon null. Bytestream not suitable for this version of DemoInfo");
                return new CS.Weapon();
            }

            CS.Weapon jwp = new CS.Weapon
            {
                //owner = assemblePlayerDetailed(wp.Owner),
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



        internal Event assemblePlayerBind(DemoInfoModded.Player player)
        {
            return new Event
            {
                gameevent = "player_bind",
                actor = assemblePlayer(player)
            };
        }

        internal Event assemblePlayerDisconnected(DemoInfoModded.Player player)
        {
            return new Event
            {
                gameevent = "player_disconnected",
                actor = assemblePlayer(player)
            };
        }
    }


}