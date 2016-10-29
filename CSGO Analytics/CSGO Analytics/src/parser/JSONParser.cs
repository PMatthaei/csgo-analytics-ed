using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DemoInfoModded;
using Newtonsoft.Json;
using demojsonparser.src.JSON.objects;
using demojsonparser.src.JSON.events;
using demojsonparser.src.JSON.objects.subobjects;

namespace demojsonparser.src.JSON
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


        public JSONGamemeta assembleGamemeta(string mapname, float tickrate, IEnumerable<Player> players)
        {
            return new JSONGamemeta
            {
                gamestate_id = 0,
                mapname = mapname,
                tickrate = tickrate,
                players = assemblePlayers(players),
            };
        }


        #region Gameevents

        public JSONPlayerKilled assemblePlayerKilled(PlayerKilledEventArgs pke)
        {
            return new JSONPlayerKilled
            {
                gameevent = "player_killed",
                attacker = assemblePlayerDetailed(pke.Killer),
                victim = assemblePlayerDetailed(pke.Victim),
                headhshot = pke.Headshot,
                penetrated = pke.PenetratedObjects,
                hitgroup = 0,
                weapon = assembleWeapon(pke.Weapon)
            };
        }

        public JSONWeaponFire assembleWeaponFire(WeaponFiredEventArgs we)
        {
            return new JSONWeaponFire
            {
                gameevent = "weapon_fire",
                shooter = assemblePlayerDetailed(we.Shooter),
                weapon = assembleWeapon(we.Weapon)
            };
        }

        public JSONPlayerMovement assemblePlayerSpotted(PlayerSpottedEventArgs e)
        {
            return new JSONPlayerMovement
            {
                gameevent = "player_spotted",
                player = assemblePlayerDetailed(e.player),
            };
        }

        public JSONWeaponFire assembleWeaponFireEmpty(WeaponFiredEmptyEventArgs we)
        {
            return new JSONWeaponFire
            {
                gameevent = "weapon_fire_empty",
                shooter = assemblePlayerDetailed(we.Shooter),
                weapon = assembleWeapon(we.Weapon)
            };
        }

        public JSONPlayerHurt assemblePlayerHurt(PlayerHurtEventArgs phe)
        {
            JSONPlayerHurt ph = new JSONPlayerHurt
            {
                gameevent = "player_hurt",
                attacker = assemblePlayerDetailed(phe.Attacker),
                victim = assemblePlayerDetailed(phe.Player),
                armor = phe.Armor,
                armor_damage = phe.ArmorDamage,
                HP = phe.Health,
                HP_damage = phe.HealthDamage,
                hitgroup = phe.Hitgroup.ToString(),
                weapon = assembleWeapon(phe.Weapon)
            };
            return ph;
        }

        public JSONPlayerFootstep assemblePlayerPosition(Player p)
        {
            return new JSONPlayerFootstep
            {
                gameevent = "player_position",
                player = assemblePlayer(p)
            };
        }

        #region Nades

        public JSONNade assembleNade(NadeEventArgs e, string eventname)
        {
            Player[] ps = null;

            if (e.GetType() == typeof(FlashEventArgs)) //Exception for FlashEvents -> we need flashed players
            {
                FlashEventArgs f = e as FlashEventArgs;
                ps = f.FlashedPlayers;
                return new JSONFlashNade
                {
                    gameevent = eventname,
                    thrownby = assemblePlayerDetailed(e.ThrownBy),
                    nadetype = e.NadeType.ToString(),
                    position = new JSONPosition3D { x = e.Position.X, y = e.Position.Y, z = e.Position.Z },
                    flashedplayers = assembleFlashedPlayers(f.FlashedPlayers)
                };
            }

            return new JSONNade
            {
                gameevent = eventname,
                thrownby = assemblePlayerDetailed(e.ThrownBy),
                nadetype = e.NadeType.ToString(),
                position = new JSONPosition3D { x = e.Position.X, y = e.Position.Y, z = e.Position.Z },
            };
        }


        #endregion

        #region Bombevents

        public JSONBomb assembleBomb(BombEventArgs be, string gameevent)
        {
            return new JSONBomb
            {
                gameevent = gameevent,
                site = be.Site,
                player = assemblePlayerDetailed(be.Player)
            };
        }

        public JSONBombState assembleBombState(BombDropEventArgs be, string gameevent)
        {
            return new JSONBombState
            {
                gameevent = gameevent,
                player = assemblePlayerDetailed(be.Player)
            };
        }

        public JSONBombState assembleBombState(BombPickUpEventArgs be, string gameevent)
        {
            return new JSONBombState
            {
                gameevent = gameevent,
                player = assemblePlayerDetailed(be.Player)
            };
        }

        public JSONBomb assembleBombDefuse(BombDefuseEventArgs bde, string gameevent)
        {
            return new JSONBomb
            {
                gameevent = gameevent,
                site = bde.Site,
                player = assemblePlayerDetailed(bde.Player),
                haskit = bde.HasKit
            };
        }
        #endregion

        #endregion



        #region SUBEVENTS

        public List<JSONPlayer> assemblePlayers(Player[] ps)
        {
            if (ps == null)
                return null;
            List<JSONPlayer> players = new List<JSONPlayer>();
            foreach (var player in ps)
                players.Add(assemblePlayer(player));

            return players;
        }

        public List<JSONPlayerFlashed> assembleFlashedPlayers(Player[] ps)
        {
            if (ps == null)
                return null;
            List<JSONPlayerFlashed> players = new List<JSONPlayerFlashed>();
            foreach (var player in ps)
                players.Add(assembleFlashPlayer(player));

            return players;
        }

        private JSONPlayerFlashed assembleFlashPlayer(Player p)
        {
            JSONPlayerFlashed player = new JSONPlayerFlashed
            {
                playername = p.Name,
                player_id = p.EntityID,
                position = new JSONPosition3D { x = p.Position.X, y = p.Position.Y, z = p.Position.Z },
                facing = new JSONFacing { yaw = p.ViewDirectionY, pitch = p.ViewDirectionX },
                team = p.Team.ToString(),
                flashedduration = p.FlashDuration
            };
            return player;
        }

        internal JSONGameevent assemblePlayerJumped(PlayerJumpedEventArgs e)
        {
            return new JSONPlayerMovement
            {
                gameevent = "player_jumped",
                player = assemblePlayerDetailed(e.Jumper)
            };
        }

        internal JSONGameevent assemblePlayerFallen(PlayerFallEventArgs e)
        {
            return new JSONPlayerMovement
            {
                gameevent = "player_fallen",
                player = assemblePlayerDetailed(e.Fallen)
            };
        }

        internal JSONGameevent assembleWeaponReload(WeaponReloadEventArgs we)
        {
            return new JSONPlayerMovement
            {
                gameevent = "weapon_reload",
                player = assemblePlayerDetailed(we.Actor)
            };
        }

        internal JSONGameevent assemblePlayerStepped(PlayerSteppedEventArgs e)
        {
            return new JSONPlayerMovement
            {
                gameevent = "player_footstep",
                player = assemblePlayerDetailed(e.Stepper)
            };
        }

        public List<JSONPlayerMeta> assemblePlayers(IEnumerable<Player> ps)
        {
            if (ps == null)
                return null;
            List<JSONPlayerMeta> players = new List<JSONPlayerMeta>();
            foreach (var player in ps)
                players.Add(assemblePlayerMeta(player));

            return players;
        }


        public JSONPlayer assemblePlayer(Player p)
        {
            JSONPlayer player = new JSONPlayer
            {
                playername = p.Name,
                player_id = p.EntityID,
                position = new JSONPosition3D { x = p.Position.X, y = p.Position.Y, z = p.Position.Z },
                facing = new JSONFacing { yaw = p.ViewDirectionY, pitch = p.ViewDirectionX },
                team = p.Team.ToString()
            };
            return player;
        }

        public JSONPlayerMeta assemblePlayerMeta(Player p)
        {
            JSONPlayerMeta player = new JSONPlayerMeta
            {
                playername = p.Name,
                player_id = p.EntityID,
                team = p.Team.ToString(),
                clanname = p.AdditionaInformations.Clantag,
                steam_id = p.SteamID,
            };
            return player;
        }

        public JSONPlayerDetailed assemblePlayerDetailed(Player p)
        {
            JSONPlayerDetailed playerdetailed = new JSONPlayerDetailed
            {
                playername = p.Name,
                player_id = p.EntityID,
                position = new JSONPosition3D { x = p.Position.X, y = p.Position.Y, z = p.Position.Z },
                facing = new JSONFacing { yaw = p.ViewDirectionY, pitch = p.ViewDirectionX },
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


        public JSONPlayerDetailedWithItems assemblePlayerDetailedWithItems(Player p)
        {
            JSONPlayerDetailedWithItems playerdetailed = new JSONPlayerDetailedWithItems
            {
                playername = p.Name,
                player_id = p.EntityID,
                position = new JSONPosition3D { x = p.Position.X, y = p.Position.Y, z = p.Position.Z },
                facing = new JSONFacing { yaw = p.ViewDirectionY, pitch = p.ViewDirectionX },
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

        public List<JSONItem> assembleWeapons(IEnumerable<Equipment> wps)
        {
            List<JSONItem> jwps = new List<JSONItem>();
            foreach (var w in wps)
                jwps.Add(assembleWeapon(w));

            return jwps;
        }

        public JSONItem assembleWeapon(Equipment wp)
        {
            if (wp == null)
            {
                Console.WriteLine("Weapon null. Bytestream not suitable for this version of DemoInfo");
                return new JSONItem();
            }

            JSONItem jwp = new JSONItem
            {
                weapon = wp.Weapon.ToString(),
                ammoinmagazine = wp.AmmoInMagazine
            };

            return jwp;
        }

        #endregion

    }


}