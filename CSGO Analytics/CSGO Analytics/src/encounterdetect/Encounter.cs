using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_Analytics.src.encounterdetect
{
    class Encounter
    {
        /// <summary>
        /// Links which form this encounter
        /// </summary>
        private List<Link> links;

        /// <summary>
        /// Time encounter occured
        /// </summary>
        private DateTime timestamp;

        /// <summary>
        /// Time to die for this link
        /// </summary>
        private float TTD;
    }
}
