using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.data.gameobjects;

namespace CSGO_Analytics.src.data.gameevents
{
    public class ServerEvents : Event //TODO: Disconnects etc
    {

    }

    public class TakeOverEvent : ServerEvents //TODO: Disconnects etc
    {
        /// <summary>
        /// The player which is been taken over by a bot
        /// </summary>
        public Player taken;
    }
}
