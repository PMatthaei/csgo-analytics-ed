using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.data.gameobjects;

namespace CSGO_Analytics.src.encounterdetect
{
    enum LinkType { COMBATLINK, SUPPORTLINK };
    enum Direction { INITIATOR, RECIPIENT, NONE };

    class Link
    {
        /// <summary>
        /// Type of the link - combat or support
        /// </summary>
        private LinkType linktype;

        /// <summary>
        /// Player who initated the link by attacking or supporting
        /// </summary>
        private Player initiator;

        /// <summary>
        /// Player who recieved a support action or was attacked
        /// </summary>
        private Player recipient;

        /// <summary>
        /// Time link occured
        /// </summary>
        private DateTime timestamp;

        /// <summary>
        /// Time to die for this link
        /// </summary>
        private float TTD;


        /// <summary>
        /// To whom is the link directed - Initiator, Recipient or none(undirected)
        /// </summary>
        private Direction direction;
    }
}
