using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DemoInfo;
using Newtonsoft.Json;
using demojsonparser.src.JSON.objects;
using demojsonparser.src.JSON.events;
using demojsonparser.src.JSON.objects.subobjects;

namespace demojsonparser.src.JSON
{
    class JSONParser
    {

        StreamWriter outputStream;
        DemoParser parser;

        enum PlayerType {META, NORMAL, DETAILED, WITHEQUIPMENT };

        public JSONParser(DemoParser parser, string path)
        {
            this.parser = parser;
            string outputpath = path.Replace(".dem", "") + ".json";
            outputStream = new StreamWriter(outputpath);
        }

        public void dump(JSONGamestate gs, bool prettyjson)
        {
            Formatting f = Formatting.None;
            if (prettyjson)
                f = Formatting.Indented;

            outputStream.Write(JsonConvert.SerializeObject(gs, f));
        }
        public void dump(string s)
        {
            outputStream.Write(s);
        }

        public void stopParser()
        {
            outputStream.Close();
        }


        public JSONGamemeta assembleGamemeta()
        {
            return new JSONGamemeta
            {
                gamestate_id = 0,
                mapname = parser.Map,
                tickrate = parser.TickRate,
                players = assemblePlayers(parser.PlayingParticipants),
            };
        }


        #region Gameevents

        public JSONPlayerKilled assemblePlayerKilled(PlayerKilledEventArgs pke)
        {
            return new JSONPlayerKilled
            {
                gameevent = "player_killed",
                attacker = assemblePlayerDetailed(pke.Killer),
                victim = assemblePlayerDetailed(pke.Killer),
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

        public JSONPlayerHurt assemblePlayerHurt(PlayerHurtEventArgs phe)
        {
            JSONPlayerHurt ph = new JSONPlayerHurt
            {
                gameevent = "player_hurt",
                attacker = assemblePlayer(phe.Attacker),
                victim = assemblePlayer(phe.Player),
                armor = phe.Armor,
                armor_damage = phe.ArmorDamage,
                HP = phe.Health,
                HP_damage = phe.HealthDamage,
                hitgroup = phe.Hitgroup.ToString(),
                weapon = assembleWeapon(phe.Weapon)
            };
            return ph;
        }

        public JSONPlayerFootstep assemblePlayerFootstep(Player p)
        {
            return new JSONPlayerFootstep
            {
                gameevent = "player_footstep",
                player = assemblePlayer(p)
            };
        }

        #region Nades
        public string parseNade(EquipmentElement nadetype, Player thrownby, Vector position, Player[] ps)
        {
            JSONNades nd = assembleNade(nadetype, thrownby, position, ps);
            return JsonConvert.SerializeObject(nd, Formatting.None);
        }

        public JSONNades assembleNade(EquipmentElement nadetype, Player thrownby, Vector position, Player[] ps)
        {
            return new JSONNades
            {
                thrownby = assemblePlayer(thrownby),
                nadetype = nadetype.ToString(),
                position = new JSONPosition3D { x = position.X, y = position.Y, z = position.Z },
                flashedplayers = assemblePlayers(ps)
            };
        }

        public JSONNades assembleHEGrenade(GrenadeEventArgs he)
        {
            JSONNades n = assembleNade(he.NadeType, he.ThrownBy, he.Position, null);
            n.gameevent = "hegrenade_detonated";
            return n;
        }

        public JSONNades assembleSmokegrenade(SmokeEventArgs he)
        {
            JSONNades n = assembleNade(he.NadeType, he.ThrownBy, he.Position, null);
            n.gameevent = "smokegrenade_detonated";
            return n;
        }

        public JSONNades assembleFiregrenade(FireEventArgs he)
        {
            JSONNades n = assembleNade(he.NadeType, he.ThrownBy, he.Position, null);
            n.gameevent = "firegrenade_detonated";
            return n;
        }

        public JSONNades assembleFlashbang(FlashEventArgs he)
        {
            JSONNades n = assembleNade(he.NadeType, he.ThrownBy, he.Position, he.FlashedPlayers);
            n.gameevent = "flashbang_detonated";
            return n;
        }

        public JSONNades assembleDecoy(DecoyEventArgs he)
        {
            JSONNades n = assembleNade(he.NadeType, he.ThrownBy, he.Position, null);
            n.gameevent = "decoy_detonated";
            return n;
        }


        public JSONNades assembleFiregrenadeEnded(FireEventArgs he)
        {
            JSONNades n = assembleNade(he.NadeType, he.ThrownBy, he.Position, null);
            n.gameevent = "firegrenade_ended";
            return n;
        }

        public JSONNades assembleSmokegrenadeEnded(SmokeEventArgs he)
        {
            JSONNades n = assembleNade(he.NadeType, he.ThrownBy, he.Position, null);
            n.gameevent = "flashbang_ended";
            return n;
        }

        public JSONNades assembleDecoyEnded(DecoyEventArgs he)
        {
            JSONNades n = assembleNade(he.NadeType, he.ThrownBy, he.Position, null);
            n.gameevent = "decoy_ended";
            return n;
        }
        #endregion

        #region Bombevents

        public JSONBomb assembleBomb(BombEventArgs be)
        {
            return new JSONBomb
            {
                site = be.Site,
                player = assemblePlayer(be.Player)
            };
        }

        public JSONBomb assembleBombDefuse(BombDefuseEventArgs bde)
        {
            return new JSONBomb
            {
                haskit = bde.HasKit,
                player = assemblePlayer(bde.Player)
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
                steam_id = p.SteamID
            
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
                hasHelmet = p.HasHelmet,
                hasdefuser = p.HasDefuseKit,
                HP = p.HP,
                armor = p.Armor
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
                hasHelmet = p.HasHelmet,
                hasdefuser = p.HasDefuseKit,
                HP = p.HP,
                armor = p.Armor,
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