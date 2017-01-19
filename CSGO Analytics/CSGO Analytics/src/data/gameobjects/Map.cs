using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.math;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Windows;

namespace CSGO_Analytics.src.data.gameobjects
{
    public class MapCreator
    {
        /// <summary>
        /// Defines the height of a level. Meaning all points starting from lowest till lowest+levelheight are included.
        /// </summary>
        private const int LEVELHEIGHT = 120;

        private const int mapdata_width = 4500;
        private const int mapdata_height = 4500;
        private const int pos_x = -2400;
        private const int pos_y = 3383;

        private static Rect[][] map_grid;

        private const int cellwidth = 15;

        /// <summary>
        /// This function takes a list of all registered points on the map and tries to
        /// reconstruct a polygonal represenatation of the map with serveral levels
        /// </summary>
        /// <param name="ps"></param>
        public static Map createMap(List<Vector3D> ps)
        {
            // Deploy a grid over the map
            var count = 0;
            var currentx = pos_x;
            var currenty = pos_y;
            map_grid = new Rect[mapdata_width / cellwidth][];
            for (int i = 0; i < map_grid.Count(); i++)
            {
                var line = new Rect[mapdata_height / cellwidth];
                for (int j = 0; j < line.Count(); j++)
                {
                    line[j] = new Rect
                    {
                        X = currentx,
                        Y = currenty,
                        Width = cellwidth,
                        Height = cellwidth
                    };
                    count++;
                    if (currenty + cellwidth > pos_y + mapdata_height) throw new Exception("Grid out of bounds");
                    if (currentx + cellwidth <= pos_x + mapdata_width)
                        currentx += cellwidth;
                    else
                    {
                        currentx = pos_x;
                        currenty += cellwidth;
                    }
                }
                map_grid[i] = line;
            }
            Console.WriteLine("Gridcells: " + count);

            // Create the map levels 
            MapLevel[] maplevels = createMapLevels(ps);
            var map_width_x = ps.Max(point => point.x) - ps.Min(point => point.x);
            var map_width_y = ps.Max(point => point.y) - ps.Min(point => point.y);
            Console.WriteLine("Mapwidth in x-Range: " + map_width_x + " Mapwidth in y-Range: " + map_width_y);

            return new Map(map_width_x, map_width_y, maplevels);
        }

        private static MapLevel[] createMapLevels(List<Vector3D> ps)
        {
            MapLevel[] maplevels;
            int levelamount = (int)Math.Ceiling((getZRange(ps) / LEVELHEIGHT));
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
            }

            return maplevels;
        }

        /// <summary>
        /// Returns Range of Z for this set of points
        /// </summary>
        /// <returns></returns>
        public static float getZRange(List<Vector3D> ps)
        {
            return ps.Max(point => point.z) - ps.Min(point => point.z);
        }
    }

    public class Map
    {
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

        public Map(float width_x, float width_y, MapLevel[] maplevels)
        {
            this.width_x = width_x;
            this.width_y = width_y;
            this.maplevels = maplevels;
        }

        /// <summary>
        /// Returns a bounding box of the map with root at 0,0
        /// </summary>
        /// <returns></returns>
        public Rect getMapBoundingBox()
        {
            return new Rect
            {
                X = 0,
                Y = 0,
                Width = this.width_x,
                Height = this.width_y
            };
        }

    }

    public class MapLevel
    {

        /// <summary>
        /// All polygons defining this level
        /// </summary>
        private List<Polygon> polygons;
        private Polygon polygon;
        public List<Vector3D> ps;
        public MapLevel(List<Vector3D> ps)
        {
            this.ps = ps;
            //var newps = removeOutliers(ps);
            //this.polygons = findPolygonsFromPoints(ps);
            //this.polygon = findPolygonsFromPoints2(ps);
        }


        /// <summary>
        /// Removes all outliers from the point cloud ps to enhance the polygonal map representation.
        /// Outliers create scattered polygons on every level which do not represent a part of the map.
        /// </summary>
        /// <param name="ps"></param>
        /// <returns></returns>
        private List<Vector3D> removeOutliers(List<Vector3D> ps)
        {
            return null;
        }


        /// <summary>
        /// Find a polygonal representation of the points to do calculates
        /// </summary>
        /// <param name="levelpoints"></param>
        /// <returns></returns>
        private List<Polygon> findPolygonsFromPoints(List<Vector3D> levelpoints)
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

    public class MapMetaData
    {
        public string mapname { get; set; }
        public double mapcenter_x { get; set; }
        public double mapcenter_y { get; set; }
        public double scale;
        public int rotate { get; set; }
        public double zoom { get; set; }
    }

    public class MapMetaDataPropertyReader
    {
        public MapMetaData metadata;

        /// <summary>
        /// Reads a map info file "<mapname>".txt and extracts the relevant data about the map
        /// </summary>
        /// <param name="path"></param>
        public MapMetaDataPropertyReader(string path)
        {
            string line;

            var fmt = new NumberFormatInfo();
            fmt.NegativeSign = "-";

            metadata = new MapMetaData();
            StreamReader file = new StreamReader(path);
            while ((line = file.ReadLine()) != null)
            {
                var resultString = Regex.Match(line, @"-?\d+").Value; //Match negative and positive int numbers

                if (line.Contains("pos_x"))
                {
                    metadata.mapcenter_x = double.Parse(resultString, fmt);
                }
                else if (line.Contains("pos_y"))
                {
                    metadata.mapcenter_y = double.Parse(resultString, fmt);
                }
                else if (line.Contains("scale"))
                {
                    metadata.scale = Double.Parse(resultString);
                }
                else if (line.Contains("rotate"))
                {
                    metadata.rotate = Int32.Parse(resultString);
                }
                else if (line.Contains("zoom"))
                {
                    metadata.zoom = Double.Parse(resultString);
                }
            }

            file.Close();
        }
    }
}