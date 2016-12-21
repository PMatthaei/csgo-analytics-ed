using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.data.gameobjects;
using CSGO_Analytics.src.math;

namespace CSGO_Analytics.src.data.gameevents
{
    public class Event
    {
        public string gameevent { get; set; } // Just needed to build valid gameevent json-objects
        /// <summary>
        /// The actor is always the person causing this event because he did something or he rises an event(f.e. he is spotted)
        /// </summary>
        public Player actor;

        /// <summary>
        /// Get Players in this event
        /// </summary>
        /// <returns></returns>
        public virtual Player[] getPlayers() { return new Player[] { actor }; }

        public virtual Vector[] getPositions() { return new Vector[] { actor.position }; }

    }
}
