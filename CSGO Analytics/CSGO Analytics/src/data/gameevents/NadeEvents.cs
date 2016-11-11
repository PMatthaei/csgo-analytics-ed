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
    /// Nadeevents hold the start and end or explosiontime of a nade.
    /// </summary>
    class NadeEvents : Gameevent
    {
        public string nadetype { get; set; }
        public Vector position { get; set; }
        
        public override Player[] getPlayers()
        {
            return new Player[] { actor };
        }


        /// <summary>
        /// Radius in which this nade affects players
        /// </summary>
        /// <returns></returns>
        public int getEffectRadius()
        {
            switch (nadetype)
            {
                case "smoke_exploded":
                    return 5;
                case "fire_exploded":
                    return 5;
                default:
                    return 0;
            }
        }
    }

    class FlashNade : NadeEvents
    {
        public IList<PlayerFlashed> flashedplayers { get; set; }
    }
}
