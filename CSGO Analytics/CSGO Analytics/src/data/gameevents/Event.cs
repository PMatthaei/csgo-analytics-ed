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
        /// <summary>
        /// Identifier for events f.e. "player_hurt" "player_killed"
        /// </summary>
        public string gameevent { get; set; }

        /// <summary>
        /// The actor is always the person causing this event(or being updated with a event)
        /// </summary>
        public Player actor;

        /// <summary>
        /// Get players in this event
        /// </summary>
        /// <returns></returns>
        public virtual Player[] getPlayers() { return new Player[] { actor }; }

        /// <summary>
        /// Get positions tracked in the event
        /// </summary>
        /// <returns></returns>
        public virtual Vector3D[] getPositions() { return new Vector3D[] { actor.position }; }

    }
}
