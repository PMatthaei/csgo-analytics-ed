using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.data.gameobjects;
using System.Windows;

namespace CSGO_Analytics.src.math
{
    class EDMathLibrary
    {
        private const float FOVVertical = 90;


        /// <summary>
        /// Returns a list of interpolated points between start and end in given steps
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="steps"></param>
        /// <returns></returns>
        public static List<Vector3D> linear_interpolatePositions(Vector3D start, Vector3D end, float steps)
        {
            var ps = new List<Vector3D>();
            float dx = start.x - end.x;
            float dy = start.y - end.y;
            float dz = start.z - end.z;
            float currentdx = 0;
            float currentdy = 0;
            float currentdz = 0;
            int count = 0;
            while (count < steps)
            {
                currentdx = currentdx + -dx / steps;
                currentdy = currentdy + -dy / steps;
                currentdz = currentdz + -dz / steps;
                ps.Add(new Vector3D(start.x + currentdx, start.y + currentdy, start.z+currentdz));
                count++;
            }
            return ps;
        }


        /// <summary>
        /// Returns euclid distance between point p1 and p2
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static double getEuclidDistance2D(Vector3D p1, Vector3D p2)
        {
            return Math.Sqrt(Math.Pow(p1.x - p2.x, 2) + Math.Pow(p1.y - p2.y, 2));
        }

        /// <summary>
        /// Returns euclid distance in 3D space
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static double getEuclidDistance3D(Vector3D p1, Vector3D p2)
        {
            return Math.Sqrt(Math.Pow(p1.x - p2.x, 2) + Math.Pow(p1.y - p2.y, 2) + Math.Pow(p1.z - p2.z, 2));
        }

        /// <summary>
        /// Returns the offset of a the actor looking straight at reciever (line of sight) -> 5° means he looking 5° away from directly looking at him.
        /// </summary>
        /// <param name="actorV"></param>
        /// <param name="actorYaw"></param>
        /// <param name="recieverV"></param>
        /// <returns></returns>
        public static double getLoSOffset(Vector3D actorV, float actorYaw, Vector3D recieverV)
        {
            double dx = recieverV.x - actorV.x;
            double dy = recieverV.y - actorV.y;

            var aimX = (float)(actorV.x + Math.Cos(toRadian(-actorYaw))); // Aim vector from Yaw
            var aimY = (float)(actorV.y + Math.Sin(toRadian(-actorYaw)));

            var aimdx = aimX - actorV.x;
            var aimdy = aimY - actorV.y;

            double theta = ScalarProductAngle(new Vector3D(aimdx, aimdy, 0), new Vector3D((float)dx, (float)dy, 0)); // Angle between line of sight and recievervector
            return toDegree(theta);
        }

        /// <summary>
        /// Test if a vector is facing another one -> 
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public bool isFacing(Vector3D actorV, float actorYaw, Vector3D recieverV)
        {
            double dx = recieverV.x - actorV.x;
            double dy = recieverV.y - actorV.y;

            var aimX = (float)(actorV.x + Math.Cos(toRadian(-actorYaw))); // Aim vector from Yaw
            var aimY = (float)(actorV.y + Math.Sin(toRadian(-actorYaw)));

            //double theta = Math.Atan2(dy, dx);
            double theta = toDegree(ScalarProductAngle(new Vector3D(aimX, aimY, 0), new Vector3D((float)dx, (float)dy, 0)));

            if (theta < 45 || theta > -45)
                return true;
            return false;
        }

        /// <summary>
        /// Checks if reciever is within the field of view (FOV-Vertical) of the actor
        /// </summary>
        /// <param name="posP1"></param>
        /// <param name="angleV"></param>
        /// <param name="angleH"></param>
        /// <param name="posP2"></param>
        /// <returns></returns>
        public static bool isInFOV(Vector3D actorV, float actorYaw, Vector3D recieverV)
        {
            double dx = recieverV.x - actorV.x;
            double dy = recieverV.y - actorV.y;

            var aimX = (float)(actorV.x + Math.Cos(toRadian(-actorYaw))); // Aim vector from Yaw
            var aimY = (float)(actorV.y + Math.Sin(toRadian(-actorYaw)));

            var aimdx = aimX - actorV.x;
            var aimdy = aimY - actorV.y;

            double theta = ScalarProductAngle(new Vector3D(aimdx, aimdy, 0), new Vector3D((float)dx, (float)dy, 0)); // Angle between line of sight and recievervector
             //if (toDegree(theta) <= FOVVertical / 2 && toDegree(theta) >= -FOVVertical / 2 && getEuclidDistance2D(actorV, recieverV) < 500) // Max sight distance to restrict FOV
            if (toDegree(theta) <= FOVVertical / 2 && toDegree(theta) >= -FOVVertical / 2 ) // No max sight distance to restrict FOV
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
        public static bool vectorClipsSphere2D(float sphereCenterX, float sphereCenterY, float sphereRadius, Vector3D actorpos, float actorYaw)
        {
            // Yaw has to be negated (csgo -> normal)
            var aimX = (float)(actorpos.x + Math.Cos(toRadian(-actorYaw))); // Aim vector from Yaw 
            var aimY = (float)(actorpos.y + Math.Sin(toRadian(-actorYaw)));

            // compute the euclidean distance between actor and aim
            var distanceActorAim = getEuclidDistance2D(actorpos, new Vector3D(aimX, aimY, 0));

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
            var distanceEC = getEuclidDistance2D(new Vector3D((float)ex, (float)ey,0), new Vector3D(sphereCenterX,sphereCenterY,0));

            // test if the line intersects the circle
            if (distanceEC < sphereRadius)
                return true;
            else if (distanceEC == sphereRadius) // line is tangent to circle
                return true;
            else // line doesnt touch circle
                return false;
        }

        /// <summary>
        /// Tests if a point lies within a rectangle area.
        /// </summary>
        /// <param name="rectx"></param>
        /// <param name="recty"></param>
        /// <param name="rectwidth"></param>
        /// <param name="rectheight"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static bool RectContainsPoint(Rect r, Vector3D p)
        {
            return r.Contains(new Point(p.x,p.y));
        }

        /// <summary>
        /// Test if a point lies within a circle
        /// </summary>
        /// <param name="cx"></param>
        /// <param name="cy"></param>
        /// <param name="r"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static bool CircleContainsPoint(float cx, float cy, float r, Vector3D p)
        {
            return getEuclidDistance2D(new Vector3D(cx, cy, 0), p) <= r;
        }




        //
        //
        // COMPLEX HULL OF POINTS
        //
        //
        
    



    //
    //
    // BASICS
    //
    //
    public static double ScalarProductAngle(Vector3D v1, Vector3D v2)
        {
            return Math.Acos((v1.x * v2.x + v1.y * v2.y + v1.z * v2.z) / (v1.Absolute() * v2.Absolute()));
        }

        public static Vector3D CrossProduct(Vector3D v1, Vector3D v2)
        {
            return new Vector3D((v1.y * v2.z - v1.z * v2.y), (v1.z * v2.x - v1.x * v2.z), (v1.x * v2.y - v1.y * v2.x));
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
        public Vector3D getAimVector(Vector3D pos, Facing facing)
        {
            var aimX = (float)(pos.x + Math.Cos(toRadian(-facing.yaw)));// Aim vector from Yaw
            var aimY = (float)(pos.y + Math.Sin(toRadian(-facing.yaw)));

            return new Vector3D(aimX, aimY, 0); //TODO: 3D level calc is missing(z achsis change with pitch)
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
