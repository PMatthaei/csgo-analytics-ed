using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DemoInfoModded;
using Newtonsoft.Json;
using CSGO_Analytics.src.data.gamestate;
using CSGO_Analytics.src.data.gameevents;
using ED = CSGO_Analytics.src.data.gameobjects;
using CSGO_Analytics.src.math;

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


        public Gamestate deserializeGamestateString(string gamestatestring)
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

        

        public Meta assembleGamemeta(string mapname, float tickrate, IEnumerable<DemoInfoModded.Player> players)
        {
            return new Meta
            {
                gamestate_id = 0,
                mapname = mapname,
                tickrate = tickrate,
                players = assemblePlayers(players.ToArray())
            };
        }

        #region Playerevents

        internal PlayerKilled assemblePlayerKilled(PlayerKilledEventArgs pke)
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

        internal PlayerSpotted assemblePlayerSpotted(PlayerSpottedEventArgs e)
        {
            return new PlayerSpotted
            {
                gameevent = "player_spotted",
                actor = assemblePlayerDetailed(e.player)
            };
        }

        internal PlayerHurt assemblePlayerHurt(PlayerHurtEventArgs phe)
        {
            return new PlayerHurt
            {
                gameevent = "player_hurt",
                actor = assemblePlayerDetailed(phe.Attacker),
                victim = assemblePlayerDetailed(phe.Victim),
                armor = phe.Armor,
                armor_damage = phe.ArmorDamage,
                HP = phe.Health,
                HP_damage = phe.HealthDamage,
                hitgroup = (int)phe.Hitgroup,
                weapon = assembleWeapon(phe.Weapon)
            };
        }

        internal MovementEvents assemblePlayerPosition(DemoInfoModded.Player p)
        {
            return new MovementEvents
            {
                gameevent = "player_position",
                actor = assemblePlayerDetailed(p)
            };
        }
        #endregion

        #region Weaponevents
        internal WeaponFire assembleWeaponFire(WeaponFiredEventArgs we)
        {
            return new WeaponFire
            {
                gameevent = "weapon_fire",
                actor = assemblePlayerDetailed(we.Shooter),
                weapon = assembleWeapon(we.Weapon)
            };
        }

        internal WeaponFire assembleWeaponFireEmpty(WeaponFiredEmptyEventArgs we)
        {
            return new WeaponFire
            {
                gameevent = "weapon_fire_empty",
                actor = assemblePlayerDetailed(we.Shooter),
                weapon = assembleWeapon(we.Weapon)
            };
        }

        #endregion

        #region Nades

        internal NadeEvents assembleNade(NadeEventArgs e, string eventname)
        {

            if (e.GetType() == typeof(FlashEventArgs)) //Exception for FlashEvents -> we need flashed players
            {
                FlashEventArgs f = e as FlashEventArgs;
                return new FlashNade
                {
                    gameevent = eventname,
                    actor = assemblePlayerDetailed(e.ThrownBy),
                    nadetype = e.NadeType.ToString(),
                    position = new EDVector3D { X = e.Position.X, Y = e.Position.Y, Z = e.Position.Z },
                    flashedplayers = assembleFlashedPlayers(f.FlashedPlayers)
                };
            }

            return new NadeEvents
            {
                gameevent = eventname,
                actor = assemblePlayerDetailed(e.ThrownBy),
                nadetype = e.NadeType.ToString(),
                position = new EDVector3D { X = e.Position.X, Y = e.Position.Y, Z = e.Position.Z },
            };
        }


        #endregion

        #region Bombevents

        internal BombEvents assembleBomb(BombEventArgs be, string gameevent)
        {
            return new BombEvents
            {
                gameevent = gameevent,
                site = be.Site,
                actor = assemblePlayerDetailed(be.Player)
            };
        }

        internal BombState assembleBombState(BombDropEventArgs be, string gameevent)
        {
            return new BombState
            {
                gameevent = gameevent,
                actor = assemblePlayerDetailed(be.Player)
            };
        }

        internal BombState assembleBombState(BombPickUpEventArgs be, string gameevent)
        {
            return new BombState
            {
                gameevent = gameevent,
                actor = assemblePlayerDetailed(be.Player)
            };
        }

        internal BombEvents assembleBombDefuse(BombDefuseEventArgs bde, string gameevent)
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

        #region Serverevents
        internal ServerEvents assemblePlayerBind(DemoInfoModded.Player player)
        {
            Console.WriteLine("Bind: " + player.Name + " ID: " + player.SteamID);
            return new ServerEvents
            {
                gameevent = "player_bind",
                actor = assemblePlayer(player)
            };
        }

        internal ServerEvents assemblePlayerDisconnected(DemoInfoModded.Player player)
        {
            Console.WriteLine("Disconnect: " +player.Name + " ID: "+player.SteamID);
            return new ServerEvents
            { 
                gameevent = "player_disconnected",
                actor = assemblePlayer(player)
            };
        }

        internal TakeOverEvent assemblePlayerTakeOver(BotTakeOverEventArgs e)
        {
            if(e.Index != null)
                Console.WriteLine("Takeover: Taker:" + e.Taker.Name + " ID: " + e.Taker.SteamID+ " Taken:" + e.Taken.Name + " ID: " + e.Taken.SteamID + " Index:" + e.Index.Name + " ID: " + e.Index.SteamID);
            else
                Console.WriteLine("Takeover: Taker:" + e.Taker.Name + " ID: " + e.Taker.SteamID + " Taken:" + e.Taken.Name + " ID: " + e.Taken.SteamID );

            return new TakeOverEvent
            {
                gameevent = "player_takeover",
                actor = assemblePlayer(e.Taker),
                taken = assemblePlayer(e.Taken)
            };
        }

        #endregion

        #region Subevents

        internal List<ED.PlayerDetailed> assemblePlayers(DemoInfoModded.Player[] ps)
        {
            if (ps == null)
                return null;
            List<ED.PlayerDetailed> players = new List<ED.PlayerDetailed>();
            foreach (var player in ps)
                players.Add(assemblePlayerDetailed(player));

            return players;
        }

        internal ED.Player assemblePlayer(DemoInfoModded.Player p)
        {
            return new ED.Player
            {
                playername = p.Name,
                player_id = p.SteamID,
                position = new EDVector3D { X = p.Position.X, Y = p.Position.Y, Z = p.Position.Z },
                facing = new ED.Facing { Yaw = p.ViewDirectionX, Pitch = p.ViewDirectionY },
                velocity = new ED.Velocity { VX = p.Velocity.X, VY = p.Velocity.Y, VZ = p.Velocity.Z },
                team = p.Team.ToString(),
                isSpotted = p.IsSpotted,
                HP = p.HP
            };
        }

        internal List<ED.PlayerFlashed> assembleFlashedPlayers(DemoInfoModded.Player[] ps)
        {
            if (ps == null)
                return null;
            List<ED.PlayerFlashed> players = new List<ED.PlayerFlashed>();
            foreach (var player in ps)
                players.Add(assembleFlashPlayer(player));

            return players;
        }

        internal ED.PlayerFlashed assembleFlashPlayer(DemoInfoModded.Player p)
        {
            ED.PlayerFlashed player = new ED.PlayerFlashed
            {
                playername = p.Name,
                player_id = p.SteamID,
                position = new EDVector3D { X = p.Position.X, Y = p.Position.Y, Z = p.Position.Z },
                velocity = new ED.Velocity { VX = p.Velocity.X, VY = p.Velocity.X, VZ = p.Velocity.X },
                facing = new ED.Facing { Yaw = p.ViewDirectionX, Pitch = p.ViewDirectionY },
                HP = p.HP,
                isSpotted = p.IsSpotted,
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




        internal ED.PlayerMeta assemblePlayerMeta(DemoInfoModded.Player p)
        {
            return new ED.PlayerMeta
            {
                playername = p.Name,
                player_id = p.SteamID,
                team = p.Team.ToString(),
                clanname = p.AdditionaInformations.Clantag,
            };
        }

        internal ED.PlayerDetailed assemblePlayerDetailed(DemoInfoModded.Player p)
        {
            if (p == null) return null;
 
            return new ED.PlayerDetailed
            {
                playername = p.Name,
                player_id = p.SteamID,
                position = new EDVector3D { X = p.Position.X, Y = p.Position.Y, Z = p.Position.Z },
                facing = new ED.Facing { Yaw = p.ViewDirectionX, Pitch = p.ViewDirectionY },
                team = p.Team.ToString(),
                isDucking = p.IsDucking,
                isSpotted = p.IsSpotted,
                isScoped = p.IsScoped,
                isWalking = p.IsWalking,
                hasHelmet = p.HasHelmet,
                hasDefuser = p.HasDefuseKit,
                HP = p.HP,
                armor = p.Armor,
                velocity = new ED.Velocity { VX = p.Velocity.X, VY = p.Velocity.Y, VZ = p.Velocity.Z }  
            };
        }


        internal ED.PlayerDetailedWithItems assemblePlayerDetailedWithItems(DemoInfoModded.Player p)
        {
            ED.PlayerDetailedWithItems playerdetailed = new ED.PlayerDetailedWithItems
            {
                playername = p.Name,
                player_id = p.SteamID,
                position = new EDVector3D { X = p.Position.X, Y = p.Position.Y, Z = p.Position.Z },
                facing = new ED.Facing { Yaw = p.ViewDirectionX, Pitch = p.ViewDirectionY },
                team = p.Team.ToString(),
                isDucking = p.IsDucking,
                hasHelmet = p.HasHelmet,
                hasDefuser = p.HasDefuseKit,
                HP = p.HP,
                armor = p.Armor,
                velocity = new ED.Velocity { VX = p.Velocity.X, VY = p.Velocity.Y, VZ = p.Velocity.Z },
                items = assembleWeapons(p.Weapons)
            };

            return playerdetailed;
        }

        internal List<ED.Weapon> assembleWeapons(IEnumerable<Equipment> wps)
        {
            List<ED.Weapon> jwps = new List<ED.Weapon>();
            foreach (var w in wps)
                jwps.Add(assembleWeapon(w));

            return jwps;
        }

        internal ED.Weapon assembleWeapon(Equipment wp)
        {
            if (wp == null)
            {
                Console.WriteLine("Weapon null. Bytestream not suitable for this version of DemoInfo");
                return new ED.Weapon();
            }

            ED.Weapon jwp = new ED.Weapon
            {
                //owner = assemblePlayerDetailed(wp.Owner), //TODO: fill weaponcategorie and type
                name = wp.Weapon.ToString(),
                ammo_in_magazine = wp.AmmoInMagazine,
            };

            return jwp;
        }



        #endregion
    }


}