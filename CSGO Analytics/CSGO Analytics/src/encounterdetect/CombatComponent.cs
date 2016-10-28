using System;
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
    class CombatComponent
    {
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

        /// <summary>
        /// Time to die for this component
        /// </summary>
        private float TTD;

        public void initLinks()
        {
            foreach(var p in players)
            {
                //TODO: Union of their links. but therefore players need to known the link they are in
            }
        }

        public void reset()
        {
            players.Clear();
            links.Clear();
            tick_id = -1; // -1 indicates a non-initalized tickid as we wont allocate negative tickids
            TTD = -1;
        }

        override public string ToString()
        {
            string s = "Component: \n";
            foreach (var l in links)
            {
                s += l.ToString()+"\n";
            }
            return s;
        }
    }
}
