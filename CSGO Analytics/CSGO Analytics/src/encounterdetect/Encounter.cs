using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.data.gameobjects;

namespace CSGO_Analytics.src.encounterdetect
{
    class Encounter
    {
        /// <summary>
        /// Components which form this encounter
        /// </summary>
        private List<EncounterComponent> comps;

        /// <summary>
        /// Units which form this encounter
        /// </summary>
        private List<Player> units;

        /// <summary>
        /// Is this encounter closed 
        /// </summary>
        private bool isClosed;

        /// <summary>
        /// Time encounter occured
        /// </summary>
        private DateTime timestamp;

        /// <summary>
        /// Time to die for this encounter
        /// </summary>
        private float TTD;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="update"></param>
        public void update(EncounterComponent update)
        {

        }

        public bool hasTimeout()
        {
            return true;
        }
    }
}
