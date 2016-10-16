using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.data.gameobjects;

namespace CSGO_Analytics.src.encounterdetect
{
    enum GameeventType
    {
        PLAYER_HURT,
        PLAYER_KILLED,
        PLAYER_STEPPED,
        PLAYER_JUMPED,
        PLAYER_POSITIONUPDATE,
        WEAPON_FIRE,
        GRENADE_START,
        GRENADE_STOP,

    };

    class TickStream : MemoryStream
    {
        public Tick readTick()
        {
            return new Tick();
        }

        public bool hasNextTick()
        {
            return true;
        }
    }

    class Tick
    {
        /// <summary>
        /// All gameevents happend in that tick
        /// </summary>
        /// <returns></returns>
        public List<Gameevent> getGameEvents()
        {
            return null;
        }

        /// <summary>
        /// Returns all players mentioned in this tick
        /// </summary>
        /// <returns></returns>
        public List<Player> getPlayerUpdates()
        {
            return null;
        }
    }

    class Gameevent
    {
        public GameeventType gtype;
    }
}
