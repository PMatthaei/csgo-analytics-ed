using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.math;
using CSGO_Analytics.src.data.gameobjects;

namespace CSGO_Analytics.src.encounterdetect.utils
{
    /// <summary>
    /// PS: Compression of trajectories for many tracked units advised.
    /// </summary>
    public class Trajectory
    {
        /// <summary>
        /// Start time of this trajectory.
        /// </summary>
        public int starttick;

        /// <summary>
        /// Player running this trajectory
        /// </summary>
        public Player player;

        /// <summary>
        /// Hashtabe all the (time,position) pairs of this players trajectory
        /// </summary>
        private OrderedDictionary positions = new OrderedDictionary();

        /// <summary>
        /// Describes the movement of a player in one life/round
        /// </summary>
        /// <param name="start"></param>
        /// <param name="player"></param>
        public Trajectory(Player player, int starttick)
        {
            this.starttick = starttick;
            this.player = player;
        }

        public void AddPosition(int tick_id, EDVector3D pos)
        {
            positions[tick_id] = pos;
        }

        /// <summary>
        /// Get position at index. index is the tick_id
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public EDVector3D Get(int index)
        {
            return (EDVector3D)positions[index];
        }

        private void compress()
        {

        }

    }
}
