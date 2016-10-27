using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_Analytics.src.math
{
    class MathUtils
    {
        public static float tickrate;

        /// <summary>
        /// Returns the time at which a tick at tickid happend.
        /// </summary>
        /// <param name="tickid"></param>
        /// <returns></returns>
        public static float TickToTime(int tickid)
        {
            return tickid * tickrate;
        }

        /// <summary>
        /// Returns euclid distance between point p1 and p2
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static int getEuclidDistance2D(Vector p1, Vector p2)
        {
            return (int)Math.Sqrt(Math.Pow(p1.x - p2.x, 2) + Math.Pow(p1.y - p2.y, 2));
        }

        /// <summary>
        /// Returns euclid distance in 3D space
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static int getEuclidDistance3D(Vector p1, Vector p2)
        {
            return (int)Math.Sqrt(Math.Pow(p1.x - p2.x, 2) + Math.Pow(p1.y - p2.y, 2) + Math.Pow(p1.z - p2.z, 2));
        }

        /// <summary>
        /// Test if a vector is facing another one
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public bool isFacing(Vector v, Vector v2)
        {
            //Calculate dot. If angle is impossible to see -> return false
            return false;
        }

        /// <summary>
        /// Checks if a position is within a certain field of view
        /// </summary>
        /// <param name="posP1"></param>
        /// <param name="angleV"></param>
        /// <param name="angleH"></param>
        /// <param name="posP2"></param>
        /// <returns></returns>
        public static bool FOVcontainsPoint(Vector posP1, float FOVVertical, float FOVHorizontal, Vector posP2)
        {
            return false;
        }

        /// <summary>
        /// Tests if a vector clips a sphere (Smoke grenade)
        /// </summary>
        /// <param name="posP1"></param>
        /// <param name="angleV"></param>
        /// <param name="angleH"></param>
        /// <param name="posP2"></param>
        /// <returns></returns>
        public static bool vectorClipsSphere()
        {
            return false;
        }
    }
}
