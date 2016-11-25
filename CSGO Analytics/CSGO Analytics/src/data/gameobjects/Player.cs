﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.math;
using Newtonsoft.Json;

namespace CSGO_Analytics.src.data.gameobjects
{
    public enum Team { None, CT, T}

    public class PlayerMeta
    {
        public string playername { get; set; }
        public int player_id { get; set; }
        public string team { get; set; }
        public string clanname { get; set; }
        public long steam_id { get; set; }
    }

    public class Player
    {
        public string playername { get; set; }
        public int player_id { get; set; }
        public string team { get; set; }
        public Vector position { get; set; }
        public Facing facing { get; set; }
        public bool isSpotted { get; set; }

        /// <summary>
        /// Maps strings back to Team.Enum
        /// </summary>
        /// <returns></returns>
        public Team getTeam()
        {
            if(team == "Terrorist")
            {
                return Team.T;
            } else if(team == "CounterTerrorist")
            {
                return Team.CT;
            }
            return Team.None;

        }

        public bool sameTeam(Player p)
        {
            if (getTeam() == p.getTeam())
                return true;

            return false;
        }

        //[JsonIgnore]
        public override bool Equals(object obj) //Why does a true overriden Equals kill the json serialisation?!?
        {
            Player p = obj as Player;
            if (p == null)
                return false;
            if (player_id == p.player_id && playername == p.playername && team == p.team)
                return true;

            return false;
        }

        public override int GetHashCode()
        {
            return this.player_id.GetHashCode();
        }

        public Player Copy()
        {
            Player me = new Player();
            me.player_id = player_id;

            me.playername = playername;
            me.team = team;
            me.position = position.Copy();
            me.facing = facing.Copy();


            return me;
        }
    }

    public class Facing
    {
        public float yaw { get; set; }
        public float pitch { get; set; }

        internal Facing Copy()
        {
            return new Facing() { yaw = yaw, pitch = pitch };
        }

        internal float[] getAsArray()
        {
            return new float[] { yaw, pitch };
        }
    }

    class PlayerDetailed : Player
    {
        public int HP { get; set; }
        public int armor { get; set; }
        public bool hasHelmet { get; set; }
        public bool hasDefuser { get; set; }
        public bool hasBomb { get; set; }
        public bool isDucking { get; set; }
        public bool isWalking { get; set; }
        public bool isScoped { get; set; }
        public double velocity { get; set; }

        public bool isDead()
        {
            if (HP == 0)
                return true;
            else
                return false;
        }
    }

    class PlayerDetailedWithItems : PlayerDetailed
    {
        public List<Weapon> items { get; set; }

        public Weapon getPrimaryWeapon()
        {
            if (items[0] == null)
                //errorlog
                return null;

            return items[0];
        }
    }

    public class PlayerFlashed : Player
    {
        public float flashedduration { get; set; }
    }

}
