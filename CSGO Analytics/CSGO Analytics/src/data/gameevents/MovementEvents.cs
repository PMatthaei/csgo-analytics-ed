using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.data.gameobjects;
using CSGO_Analytics.src.math;

namespace CSGO_Analytics.src.data.gameevents
{
    /// <summary>
    /// Movementevents just give us a hint that we should update the current position of this player.
    /// </summary>
    class MovementEvents : Event
    {

        public override Player[] getPlayers()
        {
            return new Player[] { actor };
        }

        public override Vector3D[] getPositions()
        {
            return new Vector3D[] { actor.position };
        }
    }
}
