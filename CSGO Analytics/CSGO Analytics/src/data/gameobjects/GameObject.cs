using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.math;

namespace CSGO_Analytics.src.data.gameobjects
{
    class GameObject
    {
        /// <summary>
        /// Name of the object
        /// </summary>
        private string name;

        /// <summary>
        /// ID given by CS:GO and parsed by DemoInfo
        /// </summary>
        private int entityid;

        /// <summary>
        /// Current position of the gameobject
        /// </summary>
        private Vector position;

        /// <summary>
        /// Time till the object loses relevance and needs to be destroyed(nades, projectiles etc)
        /// </summary>
        private float TTD;

        public int getID()
        {
            return entityid;
        }

        public Vector getPosition()
        {
            return position;
        }
    }
}
