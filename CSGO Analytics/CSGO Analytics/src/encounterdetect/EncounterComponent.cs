using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.data.gameobjects;

namespace CSGO_Analytics.src.encounterdetect
{
    enum ComponentType { COMBATLINK, SUPPORTLINK };
    enum Direction { UNDIRECTED, DEFAULT };

    class EncounterComponent
    {
        /// <summary>
        /// Type of the component - combat or supportlink
        /// </summary>
        private ComponentType componentType;

        /// <summary>
        /// Player who initated the component by attacking or supporting
        /// </summary>
        private Player initiator;

        /// <summary>
        /// Player who recieved a support action or was attacked
        /// </summary>
        private Player recipient;

        /// <summary>
        /// Time component occured
        /// </summary>
        private DateTime timestamp;

        /// <summary>
        /// Time to die for this component
        /// </summary>
        private float TTD;


        /// <summary>
        /// To whom is the link directed - Initiator, Recipient or none(undirected)
        /// </summary>
        private Direction direction;

    }
}
