using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.data.gameobjects;

namespace CSGO_Analytics.src.data.gameevents
{
    /// <summary>
    /// Movementevents just give us a hint that we should update the current position of this player.
    /// </summary>
    class MovementEvents : Gameevent
    {

        public override Player[] getPlayers()
        {
            return new Player[] { actor };
        }
    }
}
