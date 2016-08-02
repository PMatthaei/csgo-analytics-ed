using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGOStatsED.src.data.gameobjects
{
    class GameObject
    {
        /// <summary>
        /// Time till the object loses relevance and needs to be destroyed(nades, projectiles etc)
        /// </summary>
        public TimeSpan t;
    }
}
