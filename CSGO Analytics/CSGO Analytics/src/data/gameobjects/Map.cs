using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.math;

namespace CSGO_Analytics.src.data.gameobjects
{
    public class Map
    {
        private const int LEVELHEIGHT = 120;
        /// <summary>
        /// Array holding the different maplevels ordered from lowest level (tunnels beneth the ground) to highest ( 2nd floor etc)
        /// </summary>
        public MapLevel[] maplevels;
        public float[] maplevels_min_z;
        public float[] maplevels_max_z;

        /// <summary>
        /// Width in x range
        /// </summary>
        private float width_x;

        /// <summary>
        /// Width in y range
        /// </summary>
        private float width_y;

        public Map()
        {

        }

        public Map(float width_x, float width_y, MapLevel[] maplevels)
        {
            this.width_x = width_x;
            this.width_y = width_y;
            this.maplevels = maplevels;
        }



        /// <summary>
        /// This function takes a list of all registered points on the map and tries to
        /// reconstruct a polygonal represenatation of the map with serveral levels
        /// </summary>
        /// <param name="ps"></param>
        public Map createMapData(List<Vector> ps)
        {

            int levelamount = (int)Math.Ceiling((getZRange(ps) / LEVELHEIGHT));
            maplevels_min_z = new float[levelamount];
            maplevels_max_z = new float[levelamount];
            maplevels = new MapLevel[levelamount];

            Console.WriteLine("Levels to create: " + levelamount);
            var min_z = ps.Min(point => point.z);
            var max_z = ps.Max(point => point.z);
            Console.WriteLine("From Min Z: " + min_z + " to Max Z: " + max_z);

            for (int i = 0; i < levelamount; i++)
            {
                var upperbound = min_z + (i + 1) * LEVELHEIGHT;
                var lowerbound = min_z + i * LEVELHEIGHT;
                var levelps = ps.Where(point => point.z >= lowerbound && point.z <= upperbound).OrderBy(point => point.z);
                Console.WriteLine("Z Range for Level " + i + " between " + lowerbound + " and " + upperbound);

                if (levelps.Count() == 0)
                {
                    Console.WriteLine("No points on level:" + i);
                    continue;
                }

                Console.WriteLine("Level " + i + ": " + levelps.Count() + " points");
                Console.WriteLine("Level " + i + " starts: " + levelps.First().ToString());
                Console.WriteLine("Level " + i + " ends: " + levelps.Last().ToString());
                maplevels[i] = new MapLevel(levelps.ToList());
                maplevels_min_z[i] = 0;
                maplevels_max_z[i] = 0;

            }
            var map_width_x = ps.Max(point => point.x) - ps.Min(point => point.x);
            var map_width_y = ps.Max(point => point.y) - ps.Min(point => point.y);
            Console.WriteLine("Mapwidth in x-Range: " + map_width_x + " Mapwidth in y-Range: " + map_width_y);
            return new Map(map_width_x, map_width_y, maplevels);
        }

        /// <summary>
        /// Returns a bounding box of the map with root at 0,0
        /// </summary>
        /// <returns></returns>
        public Rectangle getMapBoundingBox()
        {
            return new Rectangle
            {
                x = 0,
                y = 0,
                width = this.width_x,
                height = this.width_y
            };
        }

        /// <summary>
        /// Returns Range of Z for this maplevel
        /// </summary>
        /// <returns></returns>
        public static float getZRange(List<Vector> ps)
        {
            return ps.Max(point => point.z) - ps.Min(point => point.z);
        }
    }

    public class MapLevel
    {

        /// <summary>
        /// All polygons defining this level
        /// </summary>
        //private List<Vector> ps;
        private List<Polygon> polygons;
        private Polygon polygon;

        public MapLevel(List<Vector> ps)
        {
            //this.ps = ps;
            //this.polygons = findPolygonsFromPoints(ps);
            //this.polygon = findPolygonsFromPoints2(ps);
        }




        /// <summary>
        /// Find a polygonal representation of the points to do calculates
        /// </summary>
        /// <param name="levelpoints"></param>
        /// <returns></returns>
        private List<Polygon> findPolygonsFromPoints(List<Vector> levelpoints)
        {
            int count = 0;
            List<Triangle> triangles = new List<Triangle>();
            foreach (var p in levelpoints) // Foreach point search 2 nearest points and build a triangle polygon
            {
                var nearestpoints = levelpoints.Where(other => EDMathLibrary.getEuclidDistance2D(other, p) < 50).OrderBy(other => EDMathLibrary.getEuclidDistance2D(other, p)).ToList();
                if (nearestpoints.Count >= 2)
                {
                    triangles.Add(new Triangle
                    {
                        NODE1 = p,
                        NODE2 = nearestpoints[0],
                        NODE3 = nearestpoints[1]
                    });
                    triangles.Add(new Triangle
                    {
                        NODE1 = p,
                        NODE2 = nearestpoints[0],
                        NODE3 = nearestpoints[1]
                    });
                }
                else
                    count++;
            }
            Console.WriteLine(triangles.Count + " Triangles from " + levelpoints.Count + " Points");
            Console.WriteLine(triangles.Distinct().Count() + " Distinct Triangles");
            Console.WriteLine("Triangle with not enough neighbors: " + count);
            count = 0;
            var finaltriangles = mergeTriangles(triangles);

            Console.WriteLine("Levelpolygons: " + finaltriangles.Count);
            foreach (var t in finaltriangles)
                count += t.ps.Count;
            Console.WriteLine("Total Points: " + count);

            return finaltriangles;
        }

        private List<Polygon> mergeTriangles(List<Triangle> triangles)
        {
            return new List<Polygon>();
        }

        public List<Polygon> getLevelPolygons()
        {
            return polygons;
        }
    }
}