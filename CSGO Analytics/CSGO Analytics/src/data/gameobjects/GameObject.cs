using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_Analytics.src.data.gameobjects
{
    class GameObject
    {
        /// <summary>
        /// Name of the object
        /// </summary>
        private string name;

        /// <summary>
        /// Owner of the object if necessary
        /// </summary>
        private Player owner;

        /// <summary>
        /// ID given by CS:GO and parsed by DemoInfo
        /// </summary>
        private int entityid;

        //private position;

        //private hitbox;

        /// <summary>
        /// Time till the object loses relevance and needs to be destroyed(nades, projectiles etc)
        /// </summary>
        private float TTD;
    }
}
