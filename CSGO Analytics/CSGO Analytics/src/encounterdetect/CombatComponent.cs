﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.data.gameobjects;

namespace CSGO_Analytics.src.encounterdetect
{
    /// <summary>
    /// Subgraph of an Encountergraph
    /// </summary>
    public class CombatComponent
    {
        public Encounter parent; // "Pointer" to encounter = parent of this component

        /// <summary>
        /// Set of players participating in the component
        /// </summary>
        public List<Player> players;

        /// <summary>
        /// Set of combat and supportlinks forming this component between all players from players
        /// </summary>
        public List<Link> links;

        /// <summary>
        /// Tick in which this component was built
        /// </summary>
        public int tick_id;

        public int contained_kill_events, contained_hurt_events, contained_spotted_events, contained_weaponfire_events;

        public CombatComponent()
        {
            this.players = new List<Player>();
        }

        public CombatComponent(int tick_id, List<Link> links)
        {
            this.tick_id = tick_id;
            this.players = new List<Player>();
            this.links = links;
        }

        public void assignPlayers()
        {
            if (links.Count == 0 || links == null) throw new Exception("No links to assign players");

            foreach (var l in links)
            {
                players.Add(l.getActor());
                players.Add(l.getReciever());
            }
            if (players.Count == 0) throw new Exception("No players assigned");

        }


        public void reset()
        {
            players.Clear();
            links.Clear();
            tick_id = -1; // -1 indicates a non-initalized tickid as we wont allocate negative tickids
        }

        override public string ToString()
        {
            string s = "Component-ID: " + tick_id + " Killevents: "+contained_kill_events+" Hurtevents: "+contained_hurt_events+" Spottedevents: "+contained_spotted_events;
            /*foreach (var l in links)
            {
                s += l.ToString() + "\n";
            }*/
            return s;
        }

        override public bool Equals(object other)
        {
            var c = other as CombatComponent;
            if (c == null)
                return false;

            if (this.tick_id == c.tick_id)
                return true;

            /*var intersection = links.Intersect(c.links);

            if (intersection.Count() == links.Count && intersection.Count() == c.links.Count)
                return true;*/

            return false;
        }

        public override int GetHashCode()
        {
            return tick_id;
        }

    }
}
