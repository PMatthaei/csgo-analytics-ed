using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using CSGO_Analytics.src.data.gameobjects;
using CSGO_Analytics.src.utils;

namespace CSGO_Analytics.src.math
{
    class MathLibrary
    {
        private const float FOVVertical = 90;
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

        public static void initalizeConstants(MapMetaData metadata) //TODO: initalize this with Data read from files about the current maps
        {
            map_origin = new Vector((float)metadata.mapcenter_x, (float)metadata.mapcenter_y, 0);
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
        public static double getEuclidDistance2D(Vector p1, Vector p2)
        {
            return Math.Sqrt(Math.Pow(p1.x - p2.x, 2) + Math.Pow(p1.y - p2.y, 2));
        }

        /// <summary>
        /// Returns euclid distance in 3D space
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static double getEuclidDistance3D(Vector p1, Vector p2)
        {
            return Math.Sqrt(Math.Pow(p1.x - p2.x, 2) + Math.Pow(p1.y - p2.y, 2) + Math.Pow(p1.z - p2.z, 2));
        }

        public static double getLineOfSightOffset(Vector actorV, float actorYaw, Vector recieverV)
        {
            double dx = recieverV.x - actorV.x;
            double dy = recieverV.y - actorV.y;

            var aimX = (float)(actorV.x + Math.Cos(toRadian(-actorYaw))); // Aim vector from Yaw
            var aimY = (float)(actorV.y + Math.Sin(toRadian(-actorYaw)));

            var aimdx = aimX - actorV.x;
            var aimdy = aimY - actorV.y;

            double theta = ScalarProductAngle(new Vector(aimdx, aimdy, 0), new Vector((float)dx, (float)dy, 0)); // Angle between line of sight and recievervector
            return toDegree(theta);
        }
        /// <summary>
        /// Test if a vector is facing another one -> 
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public bool isFacing(Vector actorV, float actorYaw, Vector recieverV)
        {
            double dx = recieverV.x - actorV.x;
            double dy = recieverV.y - actorV.y;

            var aimX = (float)(actorV.x + Math.Cos(toRadian(-actorYaw))); // Aim vector from Yaw
            var aimY = (float)(actorV.y + Math.Sin(toRadian(-actorYaw)));

            //double theta = Math.Atan2(dy, dx);
            double theta = toDegree(ScalarProductAngle(new Vector(aimX, aimY, 0), new Vector((float)dx, (float)dy, 0)));

            if (theta < 45 || theta > -45)
                return true;
            return false;
        }

        /// <summary>
        /// Checks if reciever is within the field of view (FOVVertical) of the actor
        /// </summary>
        /// <param name="posP1"></param>
        /// <param name="angleV"></param>
        /// <param name="angleH"></param>
        /// <param name="posP2"></param>
        /// <returns></returns>
        public static bool isInFOV(Vector actorV, float actorYaw, Vector recieverV)
        {
            double dx = recieverV.x - actorV.x;
            double dy = recieverV.y - actorV.y;

            var aimX = (float)(actorV.x + Math.Cos(toRadian(-actorYaw))); // Aim vector from Yaw
            var aimY = (float)(actorV.y + Math.Sin(toRadian(-actorYaw)));

            var aimdx = aimX - actorV.x;
            var aimdy = aimY - actorV.y;

            double theta = ScalarProductAngle(new Vector(aimdx, aimdy, 0), new Vector((float)dx, (float)dy, 0)); // Angle between line of sight and recievervector
             if (toDegree(theta) <= FOVVertical / 2 && toDegree(theta) >= -FOVVertical / 2 && getEuclidDistance2D(actorV, recieverV) < 500) // Max sight distance to restrict FOV
            //if (toDegree(theta) <= FOVVertical / 2 && toDegree(theta) >= -FOVVertical / 2 ) // No max sight distance to restrict FOV
                    return true;
            return false;
        }

        /// <summary>
        /// Tests if a vector clips a sphere simplified as a Ellipse/Circle(Smoke grenade)
        /// </summary>
        /// <param name="sphere"></param>
        /// <param name="actorpos"></param>
        /// <param name="actorYaw"></param>
        /// <returns></returns>
        public static bool vectorClipsSphere2D(float sphereCenterX, float sphereCenterY, float sphereRadius, Vector actorpos, float actorYaw)
        {
            // Yaw has to be negated (csgo -> normal)
            var aimX = (float)(actorpos.x + Math.Cos(toRadian(-actorYaw))); // Aim vector from Yaw 
            var aimY = (float)(actorpos.y + Math.Sin(toRadian(-actorYaw)));

            // compute the euclidean distance between actor and aim
            var distanceActorAim = getEuclidDistance2D(actorpos, new Vector(aimX, aimY, 0));

            // compute the direction vector D from Actor to aimvector
            var dx = (actorpos.x - aimX) / distanceActorAim;
            var dy = (actorpos.y - aimY) / distanceActorAim;

            // Now the line equation is x = dx*t + aimX, y = dy*t + aimY with 0 <= t <= 1.
            // compute the value t of the closest point to the circle center (Cx, Cy)
            var t = dx * (sphereCenterX - aimX) + dy * (sphereCenterY - aimY);

            // This is the projection of C on the line from actor to aim.
            // compute the coordinates of the point E on line and closest to C
            var ex = t * dx + aimX;
            var ey = t * dy + aimY;

            // compute the euclidean distance from E to C
            var distanceEC = getEuclidDistance2D(new Vector((float)ex, (float)ey,0), new Vector(sphereCenterX,sphereCenterY,0));

            // test if the line intersects the circle
            if (distanceEC < sphereRadius)
                return true;
            else if (distanceEC == sphereRadius) // line is tangent to circle
                return true;
            else // line doesnt touch circle
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
        public static double ScalarProductAngle(Vector v1, Vector v2)
        {
            return Math.Acos((v1.x * v2.x + v1.y * v2.y + v1.z * v2.z) / (v1.Absolute() * v2.Absolute()));
        }

        public static Vector CrossProduct(Vector v1, Vector v2)
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
            var aimX = (float)(pos.x + Math.Cos(toRadian(-facing.yaw)));// Aim vector from Yaw
            var aimY = (float)(pos.y + Math.Sin(toRadian(-facing.yaw)));

            return new Vector(aimX, aimY, 0); //TODO: 3D level calc is missing(z achsis change with pitch)
        }

        /// <summary>
        /// Alternative for cantor pairing function: http://stackoverflow.com/questions/919612/mapping-two-integers-to-one-in-a-unique-and-deterministic-way
        /// Used to distinct links with their participants ids
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public int SzudzikFunction(int a, int b)
        {
            if (a >= 0 && b >= 0)
                return a >= b ? a * a + a + b : a + b * b;
            else
                return -1;
        }

    }

}
