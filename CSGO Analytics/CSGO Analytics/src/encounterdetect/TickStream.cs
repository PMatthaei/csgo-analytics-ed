using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.data.gameobjects;

namespace CSGO_Analytics.src.encounterdetect
{
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
        public List<Gameevent> getGameEvents()
        {
            return null;
        }

        public List<Player> getPlayerUpdates()
        {
            return null;
        }
    }

    class Gameevent
    {

    }
}
