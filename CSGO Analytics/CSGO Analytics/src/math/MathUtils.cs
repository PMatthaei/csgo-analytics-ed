using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace CSGO_Analytics.src.math
{
    class MathUtils
    {


        /// <summary>
        /// Converts a CS:GO Position fetched from a replay file into a coordinate for our UI
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Vector CSPositionToUIPosition(Vector p)
        {
            var x = p.x;
            var y = p.y;
            return new Vector(x, y, 0);
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
        public bool isFacing(Vector actorV, float actorYaw, Vector recieverV)
        {
            double dx = recieverV.x - actorV.x;
            double dy = recieverV.y - actorV.y;

            var aimX = (float)(actorV.x + Math.Cos(actorYaw)); // Aim vector from Yaw
            var aimY = (float)(actorV.y + Math.Sin(actorYaw));

            double aimdx = aimX - actorV.x;
            double aimdy = aimY - actorV.y;

            //double theta = Math.Atan2(dy, dx);
            double theta = toDegree( ScalarProduct(new Vector(aimX, aimY, 0), new Vector((float)dx, (float)dy, 0)) );

            if (theta < 45 && theta > -45)
                return true;
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
        public static bool FOVcontainsPoint(Vector actorV, float actorYaw, Vector recieverV, float FOVVertical)
        {
            double dx = recieverV.x - actorV.x;
            double dy = recieverV.y - actorV.y;

            var aimX = (float)(actorV.x + Math.Cos(actorYaw)); // Aim vector from Yaw
            var aimY = (float)(actorV.y + Math.Sin(actorYaw));

            double aimdx = aimX - actorV.x;
            double aimdy = aimY - actorV.y;

            //double theta = Math.Atan2(dy, dx);
            double theta = toDegree(ScalarProduct(new Vector(aimX, aimY, 0), new Vector((float)dx, (float)dy, 0)));

            if (theta < FOVVertical && theta > -FOVVertical)
                return true;
            return false;
        }

        /// <summary>
        /// Tests if a vector clips a sphere simplified as a Ellipse/Circle(Smoke grenade)
        /// </summary>
        /// <param name="sphere"></param>
        /// <param name="actorV"></param>
        /// <param name="actorYaw"></param>
        /// <returns></returns>
        public static bool vectorClipsSphere2D(Ellipse sphere, Vector actorV, float actorYaw)
        {
            var actorposx = actorV.x; // Position of player
            var actorposy = actorV.y;

            var aimX = (float)(actorposx + Math.Cos(actorYaw)); // Aim vector from Yaw
            var aimY = (float)(actorposy + Math.Sin(actorYaw));

            var sphereCenterX = sphere.Margin.Left; // Center of the nade
            var sphereCenterY = sphere.Margin.Top;

            var sphereRadius = Math.Min(sphere.Width, sphere.Height); // Radius of the nade


            double dx = aimX - actorV.y;
            double dy = aimY - actorV.y;
            double theta = Math.Atan2(dy, dx);
            double r = getEuclidDistance2D(actorV, new Vector(aimX, aimY, 0)) - ((sphereRadius * sphereRadius) / Math.Sqrt(Math.Pow(sphereRadius * Math.Cos(theta), 2) + Math.Pow(sphereRadius * Math.Sin(theta), 2)));
            //return new Vector((float)(actorposx + r * Math.Cos(theta)), (float)(actorposy + r * Math.Sin(theta)), 0);

            return false;
        }


        /// <summary>
        /// Tests if a vector clips a sphere in 3D(Smoke grenade)
        /// </summary>
        /// <param name="sphere"></param>
        /// <param name="actorV"></param>
        /// <param name="actorYaw"></param>
        /// <returns></returns>
        public static bool vectorClipsSphere3D()
        {
            return false;
        }


        //
        //
        // BASICS
        //
        //
        private static double ScalarProduct(Vector v1, Vector v2)
        {
            return (v1.x * v2.x + v1.y * v2.y - v1.z * v2.z) / (v1.Absolute() * v2.Absolute());
        }

        private static Vector CrossProduct(Vector v1, Vector v2)
        {
            return new Vector((v1.y * v2.z - v1.z * v2.y)  ,  (v1.z* v2.x -v1.x * v2.z)  ,  (v1.x* v2.y -v1.y * v2.x) );
        }

        private static double toDegree(double radian)
        {
            return radian * 180.0 / Math.PI;
        }

        private static double toRadian(double degree)
        {
            return degree * Math.PI / 180.0;
        }
    }

}
