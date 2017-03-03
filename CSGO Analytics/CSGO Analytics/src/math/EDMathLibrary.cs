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

        private const float FOVVertical = 106; // 106 Degress Vertical Field of View for CS:GO


        /// <summary>
        /// Returns a list of interpolated points between start and end in given steps
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="steps"></param>
        /// <returns></returns>
        public static List<EDVector3D> interpolatePositionsLinear(EDVector3D start, EDVector3D end, float steps)
        {
            var ps = new List<EDVector3D>();
            float dx = start.X - end.X;
            float dy = start.Y - end.Y;
            float dz = start.Z - end.Z;
            float currentdx = 0;
            float currentdy = 0;
            float currentdz = 0;
            int count = 0;
            while (count < steps)
            {
                currentdx = currentdx + -dx / steps;
                currentdy = currentdy + -dy / steps;
                currentdz = currentdz + -dz / steps;
                ps.Add(new EDVector3D(start.X + currentdx, start.Y + currentdy, start.Z + currentdz));
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
        public static double getEuclidDistance2D(EDVector3D p1, EDVector3D p2)
        {
            if (p2 == null || p1 == null) throw new Exception("Vector cant be null");
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        /// <summary>
        /// Returns euclid distance in 3D space
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static double getEuclidDistance3D(EDVector3D p1, EDVector3D p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2) + Math.Pow(p1.Z - p2.Z, 2));
        }

        /// <summary>
        /// Returns the offset of a the actor looking straight at reciever (line of sight) -> 5° means he looking 5° away from directly looking at him.
        /// </summary>
        /// <param name="actorV"></param>
        /// <param name="actorYaw"></param>
        /// <param name="recieverV"></param>
        /// <returns></returns>
        public static double getLOSOffset(EDVector3D actorV, float actorYaw, EDVector3D recieverV)
        {
            double dx = recieverV.X - actorV.X;
            double dy = recieverV.Y - actorV.Y;

            var aimX = (float)(actorV.X + Math.Cos(toRadian(-actorYaw))); // Aim vector from Yaw
            var aimY = (float)(actorV.Y + Math.Sin(toRadian(-actorYaw)));

            var aimdx = aimX - actorV.X;
            var aimdy = aimY - actorV.Y;

            double theta = ScalarProductAngle(new EDVector3D(aimdx, aimdy, 0), new EDVector3D((float)dx, (float)dy, 0)); // Angle between line of sight and recievervector
            return toDegree(theta);
        }

        /// <summary>
        /// Test if a vector is facing another one -> 
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public bool isFacing(EDVector3D actorV, float actorYaw, EDVector3D recieverV)
        {
            double dx = recieverV.X - actorV.X;
            double dy = recieverV.Y - actorV.Y;

            var aimX = (float)(actorV.X + Math.Cos(toRadian(-actorYaw))); // Aim vector from Yaw
            var aimY = (float)(actorV.Y + Math.Sin(toRadian(-actorYaw)));

            //double theta = Math.Atan2(dy, dx);
            double theta = toDegree(ScalarProductAngle(new EDVector3D(aimX, aimY, 0), new EDVector3D((float)dx, (float)dy, 0)));

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
        public static bool isInFOV(EDVector3D actorV, float actorYaw, EDVector3D recieverV)
        {
            double dx = recieverV.X - actorV.X;
            double dy = recieverV.Y - actorV.Y;

            var aimX = (float)(actorV.X + Math.Cos(toRadian(-actorYaw))); // Aim vector from Yaw
            var aimY = (float)(actorV.Y + Math.Sin(toRadian(-actorYaw)));

            var aimdx = aimX - actorV.X;
            var aimdy = aimY - actorV.Y;

            double theta = ScalarProductAngle(new EDVector3D(aimdx, aimdy, 0), new EDVector3D((float)dx, (float)dy, 0)); // Angle between line of sight and recievervector
            //if (toDegree(theta) <= FOVVertical / 2 && toDegree(theta) >= -FOVVertical / 2 && getEuclidDistance2D(actorV, recieverV) < 500) // Max sight distance to restrict FOV
            if (toDegree(theta) <= FOVVertical / 2 && toDegree(theta) >= -FOVVertical / 2) // No max sight distance to restrict FOV
                return true;
            return false;
        }


        /// <summary>
        /// Code see wikipedia.
        /// </summary>
        /// <param name="actorpos"></param>
        /// <param name="recieverpos"></param>
        /// <param name="maplevel"></param>
        /// <returns></returns>
        public static EDVector3D BresenhamLineStepping(EDVector3D actorpos, EDVector3D recieverpos, MapLevel maplevel)
        {
            MapgridCell[][] grid = maplevel.level_grid;
            // First get Cells of actor and reciever
            var actorcell = maplevel.cells_tree.FindValueAt(actorpos.getAsDoubleArray2D());
            var recievercell = maplevel.cells_tree.FindValueAt(recieverpos.getAsDoubleArray2D());

            Console.WriteLine("Actorpos: " + actorpos);
            Console.WriteLine("Recieverpos: " + recieverpos);
            Console.WriteLine("actorcell: " + actorcell);
            Console.WriteLine("recievercell: " + recievercell);
            int stepcount = 0;

            var dx = recievercell.index_X - actorcell.index_X;
            var dy = recievercell.index_Y - actorcell.index_Y;

            var adx = Math.Abs(dx);
            var ady = Math.Abs(dy);

            var sdx = Math.Sign(dx);
            var sdy = Math.Sign(dy);

            int pdx, pdy, ddx, ddy, es, el;
            if (adx > ady)
            {
                pdx = sdy; pdy = 0;
                ddx = sdx; ddy = sdy;
                es = ady; el = adx;
            }
            else
            {
                pdx = 0; pdy = sdy;
                ddx = sdx; ddy = sdy;
                es = adx; el = ady;
            }

            var x = actorcell.index_X;
            var y = actorcell.index_Y;

            var error = el / 2;

            for (int i = 1; i < el; i++) // Iterate every point on the LOS
            {
                error -= es;
                if (error < 0)
                {
                    error += el;
                    x += ddx; y += ddy;
                }
                else
                {
                    x += pdx; y += pdy;
                }
                stepcount++;

                var celltest = grid[x][y];

                var intersectionpoint = LineIntersectsRect(actorpos, recieverpos, celltest);
                if (intersectionpoint != null)
                    return intersectionpoint;
            }
            return null;
        }

        public static EDVector3D LOSIntersectsMapBresenham(EDVector3D start, EDVector3D end, MapLevel maplevel)
        {
            return BresenhamLineStepping(start, end, maplevel);
        }

        /// <summary>
        /// Test if a vetor from actor to reciever collides with a rect representing a wall or obstacle.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="level_cells"></param>
        /// <returns></returns>
        public static EDVector3D LOSIntersectsMap(EDVector3D start, EDVector3D end, MapLevel maplevel)
        {
            //return BresenhamLineStepping(start, end, maplevel);
            if (maplevel == null) throw new Exception("Maplevel cannot be null");

            //Quadtree reduces cells to test depending on the rectangle formed by actorps and recieverpos -> players are close -> far less cells
            var searchrect = getRectFromPoints(start, end);

            var queriedRects = maplevel.walls_tree.GetObjects(searchrect.getAsQuadTreeRect());

            foreach (var wallcell in queriedRects.OrderBy(r => Math.Abs(r.Center.X - start.X)).ThenBy(r => Math.Abs(r.Center.Y - start.Y))) //Order Rectangles by distance to the actor. 
            {
                var intersection_point = LineIntersectsRect(start, end, wallcell);
                if (intersection_point != null)
                {
                    return intersection_point;
                }
            }   
            return null;


        }


        /// <summary>
        /// Tests if a vector clips a 2d sphere simplified as a Ellipse/Circle(Smoke grenade)
        /// </summary>
        /// <param name="sphere"></param>
        /// <param name="actorpos"></param>
        /// <param name="actorYaw"></param>
        /// <returns></returns>
        public static bool vectorIntersectsSphere2D(float sphereCenterX, float sphereCenterY, float sphereRadius, EDVector3D actorpos, float actorYaw)
        {
            // Yaw has to be negated (csgo -> normal)
            var aimX = (float)(actorpos.X + Math.Cos(toRadian(-actorYaw))); // Aim vector from Yaw 
            var aimY = (float)(actorpos.Y + Math.Sin(toRadian(-actorYaw)));

            // compute the euclidean distance between actor and aim
            var distanceActorAim = getEuclidDistance2D(actorpos, new EDVector3D(aimX, aimY, 0));

            // compute the direction vector D from Actor to aimvector
            var dx = (actorpos.X - aimX) / distanceActorAim;
            var dy = (actorpos.Y - aimY) / distanceActorAim;

            // Now the line equation is x = dx*t + aimX, y = dy*t + aimY with 0 <= t <= 1.
            // compute the value t of the closest point to the circle center C(sphereCenterX, sphereCenterY)
            var t = dx * (sphereCenterX - aimX) + dy * (sphereCenterY - aimY);

            // This is the projection of C on the line from actor to aim.
            // compute the coordinates of the point E on line and closest to C
            var ex = t * dx + aimX;
            var ey = t * dy + aimY;

            // compute the euclidean distance from E to C
            var distanceEC = getEuclidDistance2D(new EDVector3D((float)ex, (float)ey, 0), new EDVector3D(sphereCenterX, sphereCenterY, 0));

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
        public static bool RectContainsPoint(EDRect r, EDVector3D v)
        {
            return r.Contains(v);
        }

        /// <summary>
        /// Test if a point lies within a circle
        /// </summary>
        /// <param name="cx"></param>
        /// <param name="cy"></param>
        /// <param name="r"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static bool CircleContainsPoint(float cx, float cy, float r, EDVector3D p)
        {
            return getEuclidDistance2D(new EDVector3D(cx, cy, 0), p) <= r;
        }

        /// <summary>
        /// Intersection of a line with a rect. -> perform 4 line tests.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public static EDVector3D LineIntersectsRect(EDVector3D p1, EDVector3D p2, EDRect r)
        {
            var l1 = LineLineIntersectionPoint(p1, p2, new EDVector3D((float)r.X, (float)r.Y, 0), new EDVector3D((float)(r.X + r.Width), (float)r.Y, 0));
            var l2 = LineLineIntersectionPoint(p1, p2, new EDVector3D((float)(r.X + r.Width), (float)r.Y, 0), new EDVector3D((float)(r.X + r.Width), (float)(r.Y + r.Height), 0));
            var l3 = LineLineIntersectionPoint(p1, p2, new EDVector3D((float)(r.X + r.Width), (float)(r.Y + r.Height), 0), new EDVector3D((float)r.X, (float)(r.Y + r.Height), 0));
            var l4 = LineLineIntersectionPoint(p1, p2, new EDVector3D((float)r.X, (float)(r.Y + r.Height), 0), new EDVector3D((float)r.X, (float)r.Y, 0));

            List<EDVector3D> ps = new List<EDVector3D>();
            if (l1 != null) ps.Add(l1);//TODO: Ugly code
            if (l2 != null) ps.Add(l2);
            if (l3 != null) ps.Add(l3);
            if (l4 != null) ps.Add(l4);
            if (ps.Count == 0) return null;
            if (ps.Count > 2) throw new Exception("Too many collisions");
            return ps.OrderBy(point => getEuclidDistance2D(p1, point)).First(); // Return point with lowest distance to p1
        }


        /// <summary>
        /// Get Intersection point between to lines. line A from a1 to a2 and line B from b1 to b2
        /// </summary>
        /// <param name="a1">a1 is line1 start</param>
        /// <param name="a2">a2 is line1 end</param>
        /// <param name="b1">b1 is line2 start</param>
        /// <param name="b2">b2 is line2 end</param>
        /// <returns></returns>
        public static EDVector3D LineLineIntersectionPoint(EDVector3D a1, EDVector3D a2, EDVector3D b1, EDVector3D b2)
        {
            EDVector3D b = a2 - a1;
            EDVector3D d = b2 - b1;
            var bDotDPerp = b.X * d.Y - b.Y * d.X;

            // if b dot d == 0, it means the lines are parallel so have infinite intersection points
            if (bDotDPerp == 0)
                return null;

            EDVector3D c = b1 - a1;
            var t = (c.X * d.Y - c.Y * d.X) / bDotDPerp;
            if (t < 0 || t > 1)
            {
                return null;
            }

            var u = (c.X * b.Y - c.Y * b.X) / bDotDPerp;
            if (u < 0 || u > 1)
            {
                return null;
            }

            return a1 + t * b;
        }

        /// <summary>
        /// Get the bounding rectangle of a pointset
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static EDRect getPointCloudBoundings(List<EDVector3D> data)
        {
            var min_x = data.Min(point => point.X);
            var min_y = data.Min(point => point.Y);
            var max_x = data.Max(point => point.X);
            var max_y = data.Max(point => point.Y);
            var dx = max_x - min_x;
            var dy = max_y - min_y;
            return new EDRect { X = min_x, Y = max_y, Width = dx, Height = dy };
        }

        /// <summary>
        /// Get the bounding rectangle between to positions.
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static EDRect getRectFromPoints(EDVector3D v1, EDVector3D v2)
        {
            var min_x = Math.Min(v1.X, v2.X);
            var max_x = Math.Max(v1.X, v2.X);
            var min_y = Math.Min(v1.Y, v2.Y);
            var max_y = Math.Max(v1.Y, v2.Y);
            var dx = max_x - min_x;
            var dy = max_y - min_y;
            return new EDRect { X = min_x, Y = min_y, Width = dx, Height = dy };
        }
        //
        //
        // BASICS
        //
        //
        public static double ScalarProductAngle(EDVector3D v1, EDVector3D v2)
        {
            return Math.Acos((v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z) / (v1.Absolute() * v2.Absolute()));
        }

        public static EDVector3D CrossProduct(EDVector3D v1, EDVector3D v2)
        {
            return new EDVector3D((v1.Y * v2.Z - v1.Z * v2.Y), (v1.Z * v2.X - v1.X * v2.Z), (v1.X * v2.Y - v1.Y * v2.X));
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
        public static EDVector3D getAimVector(EDVector3D pos, Facing facing)
        {
            var aimX = (float)(pos.X + Math.Cos(toRadian(-facing.Yaw)));// Aim vector from Yaw
            var aimY = (float)(pos.Y + Math.Sin(toRadian(-facing.Yaw)));
            var aimZ = (float)(pos.Y + Math.Sin(toRadian(-facing.Pitch))); //TODO: check if valid?!?!

            return new EDVector3D(aimX, aimY, aimZ);
        }

    }

}
