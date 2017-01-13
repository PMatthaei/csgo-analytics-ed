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
        public TrajectoryLink start;
        public Player player;

        public Trajectory(TrajectoryLink start, Player player)
        {
            this.start = start;
            this.player = player;
        }

        public double PathLength()
        {
            double length = 0;
            TrajectoryLink current = start;
            Vector oldpos = current.pos;
            while(current != null)
            {
                current = current.next;
                length += EDMathLibrary.getEuclidDistance2D(oldpos, current.next.pos);
                oldpos = current.pos;
            }
            return length;
        }
    }

    public class TrajectoryLink
    {
        public TrajectoryLink next;
        public Vector pos;
        /// <summary>
        /// Id of the tick where this link was created. we need this to figure out the time it occured
        /// </summary>
        public int tick_id; 

        public TrajectoryLink(TrajectoryLink next, Vector pos)
        {
            this.next = next;
            this.pos = pos;
        }
    }

}
