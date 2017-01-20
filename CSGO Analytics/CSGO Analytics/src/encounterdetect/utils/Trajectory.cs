using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.math;
using CSGO_Analytics.src.data.gameobjects;

namespace CSGO_Analytics.src.encounterdetect.utils
{
    public class Trajectory
    {
        /// <summary>
        /// Start node of a linked list.
        /// </summary>
        public TrajectoryLink start;

        /// <summary>
        /// Player running this trajectory
        /// </summary>
        public Player player;

        /// <summary>
        /// Describes the movement of a player in one life/round
        /// </summary>
        /// <param name="start"></param>
        /// <param name="player"></param>
        public Trajectory(TrajectoryLink start, Player player)
        {
            this.start = start;
            this.player = player;
        }

        private TrajectoryLink current;

        public TrajectoryLink next()
        {
            if (current.next != null)
            {
                current = current.next;
                return current;
            }
            else 
                return null;
        }

        /// <summary>
        /// Returns the trajectory length in with summed up distances of the trajectorylinks
        /// </summary>
        /// <returns></returns>
        public double Length()
        {
            double length = 0;
            TrajectoryLink current = start;
            EDVector3D oldpos = current.pos;
            while(current != null)
            {
                current = current.next;
                length += EDMathLibrary.getEuclidDistance2D(oldpos, current.next.pos);
                oldpos = current.pos;
            }
            return length;
        }
    }

    /// <summary>
    /// Element of a linked list trajectory. Describes a point where the player of trajectory stood at tick tick_id
    /// </summary>
    public class TrajectoryLink
    {
        /// <summary>
        /// The next stepped position.
        /// </summary>
        public TrajectoryLink next;

        /// <summary>
        /// Koordinates of the step
        /// </summary>
        public EDVector3D pos;

        /// <summary>
        /// Id of the tick where this link was created. we need this to figure out the time it occured
        /// </summary>
        public int tick_id; 

        public TrajectoryLink(TrajectoryLink next, EDVector3D pos)
        {
            this.next = next;
            this.pos = pos;
        }
    }

}
