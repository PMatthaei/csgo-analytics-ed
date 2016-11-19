using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using CSGO_Analytics.src.data.gameobjects;

namespace CSGO_Analytics.src.math
{
    class MathLibrary
    {
        /// <summary>
        /// Every CSGO Map has its center from where positions are calculated. We need this to produce our own coords. This is read by PropertieReader
        /// </summary>
        private static Vector map_origin;

        //Size of Map in CSGO
        private static double map_width;
        private static double map_height;
        // Size of Image (Bitmap)
        private static double mappanel_width;
        private static double mappanel_height;

        public static void initalizeConstants() //TODO: initalize this with Data read from files about the current maps
        {
            map_origin = new Vector(-2400, 3383, 0);
            map_width = 4500;
            map_height = 4500;
            mappanel_width = 575;
            mappanel_height = 575;
        }

        /// <summary>
        /// Function getting a CS:GO Position fetched from a replay file which returns a coordinate for our UI
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Vector CSPositionToUIPosition(Vector p)
        {
            // Calculate a given demo point into a point suitable for our gui minimap: therefore we need a rotation factor, the origin of the coordinate and other data about the map. 
            var x = Math.Abs(map_origin.x - p.x) * (Math.Min(mappanel_width, map_width) / Math.Max(mappanel_width, map_width));
            var y = Math.Abs(map_origin.y - p.y) * (Math.Min(mappanel_height, map_height) / Math.Max(mappanel_height, map_height));
            return new Vector((float)x, (float)y, p.z);
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
            double theta = toDegree(ScalarProduct(new Vector(aimX, aimY, 0), new Vector((float)dx, (float)dy, 0)));

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

            if (theta < FOVVertical / 2 && theta > -FOVVertical / 2)
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
            double dr = Math.Sqrt(dx * dx + dy * dy);
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
            return new Vector((v1.y * v2.z - v1.z * v2.y), (v1.z * v2.x - v1.x * v2.z), (v1.x * v2.y - v1.y * v2.x));
        }

        public static double toDegree(double radian)
        {
            return radian * 180.0 / Math.PI;
        }

        public static double toRadian(double degree)
        {
            return degree * Math.PI / 180.0;
        }

        //
        // FUNTIONS FOR SIGHTTESTING ETC
        //
        /// <summary>
        /// Returns aimvector from a player at pos with viewangle yaw 
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="yaw"></param>
        /// <returns></returns>
        public Vector getAimVector(Vector pos, Facing facing)
        {
            var aimX = (float)(pos.x + Math.Cos(facing.yaw)); // Aim vector from Yaw
            var aimY = (float)(pos.y + Math.Sin(facing.yaw));

            return new Vector(aimX, aimY, 0); //TODO: 3D level calc is missing(z achsis change with pitch)
        }


    }

}
