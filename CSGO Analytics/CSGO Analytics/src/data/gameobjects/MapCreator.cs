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
using System.Collections;
using QuadTrees;
using QuadTrees.Common;
using FastDBScan;
using KdTree.Math;

namespace CSGO_Analytics.src.data.gameobjects
{
    public class MapCreator
    {
        /// <summary>
        /// Defines the height of a level. Meaning all points starting from lowest till lowest+levelheight are included.
        /// </summary>
        private const int LEVELHEIGHT = 1200;

        private const int mapdata_width = 4500;
        private const int mapdata_height = 4500;
        private const int pos_x = -2400;
        private const int pos_y = 3383;

        private static EDRect[][] map_grid;

        private static int cellwidth = 60;

        /// <summary>
        /// This function takes a list of all registered points on the map and tries to
        /// reconstruct a polygonal represenatation of the map with serveral levels
        /// </summary>
        /// <param name="ps"></param>
        public static Map createMap(HashSet<EDVector3D> ps)
        {
            // Deploy a grid over the map
            var currentx = pos_x;
            var currenty = pos_y;
            //map_grid = new HashSet<EDRect>();
            var cells = (mapdata_height / cellwidth) * (mapdata_width / cellwidth);

            map_grid = new EDRect[mapdata_height / cellwidth][];

            for (int k = 0; k < map_grid.Length; k++)
            {
                map_grid[k] = new EDRect[mapdata_height / cellwidth];

                for (int l = 0; l < map_grid[k].Length; l++)
                {
                    map_grid[k][l] = new EDRect
                    {
                        index_X = k,
                        index_Y = l,
                        X = currentx,
                        Y = currenty,
                        Width = cellwidth,
                        Height = cellwidth,
                        blocked = false
                    };
                    currentx += cellwidth;

                }
                currentx = pos_x;
                currenty -= cellwidth;
            }


            // Create the map levels 
            MapLevel[] maplevels = createMapLevels(ps);
            var map_width_x = ps.Max(point => point.X) - ps.Min(point => point.X);
            var map_width_y = ps.Max(point => point.Y) - ps.Min(point => point.Y);
            Console.WriteLine("Max x: " + ps.Max(point => point.X) + " Min x: " + ps.Min(point => point.X));
            Console.WriteLine("Mapwidth in x-Range: " + map_width_x + " Mapwidth in y-Range: " + map_width_y);

            return new Map(map_width_x, map_width_y, maplevels);
        }

        /// <summary>
        /// Create a maplevel according to its walkable space.
        /// </summary>
        /// <param name="ps"></param>
        /// <returns></returns>
        private static MapLevel[] createMapLevels(HashSet<EDVector3D> ps)
        {
            int levelamount = (int)Math.Ceiling((getZRange(ps) / LEVELHEIGHT));

            MapLevel[] maplevels = new MapLevel[levelamount];

            Console.WriteLine("Levels to create: " + levelamount);
            var min_z = ps.Min(point => point.Z);
            var max_z = ps.Max(point => point.Z);
            Console.WriteLine("From Min Z: " + min_z + " to Max Z: " + max_z);

            for (int i = 0; i < levelamount; i++)
            {
                var upperbound = min_z + (i + 1) * LEVELHEIGHT;
                var lowerbound = min_z + i * LEVELHEIGHT;
                var levelps = new HashSet<EDVector3D>(ps.Where(point => point.Z >= lowerbound && point.Z <= upperbound).OrderBy(point => point.Z));
                Console.WriteLine("Z Range for Level " + i + " between " + lowerbound + " and " + upperbound);

                if (levelps.Count() == 0)
                {
                    Console.WriteLine("No points on level:" + i);
                    continue;
                }

                Console.WriteLine("Level " + i + ": " + levelps.Count() + " points");
                //Console.WriteLine("Level " + i + " starts: " + levelps.First().ToString());
                //Console.WriteLine("Level " + i + " ends: " + levelps.Last().ToString());
                var ml = new MapLevel(i, lowerbound, upperbound);
                assignLevelcells(ml, levelps.ToArray());
                maplevels[i] = ml;
            }
            map_grid = null;
            return maplevels;
        }

        private const int MIN_CELL_QUERY = 1;

        /// <summary>
        /// Assgins all wall cells on a maplevel
        /// </summary>
        /// <param name="ml"></param>
        public static void assignLevelcells(MapLevel ml, EDVector3D[] points)
        {
            var count = 0;
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var dbscan = new KD_DBSCANClustering((x, y) => Math.Sqrt(((x.X - y.X) * (x.X - y.X)) + ((x.Y - y.Y) * (x.Y - y.Y))));

            ml.clusters = dbscan.ComputeClusterDbscan(allPoints: points, epsilon: 60.0, minPts: 2);
            points = null; //Collect points for garbage

            ml.level_grid = new EDRect[mapdata_height / cellwidth][];
            for (int k = 0; k < ml.level_grid.Length; k++)
                ml.level_grid[k] = new EDRect[mapdata_height / cellwidth];


            QuadTreePoint<EDVector3D> qtree = new QuadTreePoint<EDVector3D>();
            foreach (var cl in ml.clusters)
                foreach (var p in cl)
                    qtree.Add(p);

            for (int k = 0; k < map_grid.Length; k++)
                for (int l = 0; l < map_grid[k].Length; l++)
                {
                    var cell = map_grid[k][l].Copy();
                    var rectps = qtree.GetObjects(cell.getAsQuadTreeRect()); //Get points in a cell
                    if (rectps.Count >= MIN_CELL_QUERY)
                    {
                        cell.blocked = false;
                    }
                    else
                    {
                        ml.qtree.Add(cell);
                        cell.blocked = true;
                    }

                    ml.cells_tree.Add(cell.Center.getAsDoubleArray2D(), cell);
                    ml.level_grid[k][l] = cell;
                    count++;
                }


            ml.cells_tree.Balance();

            Console.WriteLine("Occupied cells by this level: " + count);
            watch.Stop();
            var sec = watch.ElapsedMilliseconds / 1000.0f;
            Console.WriteLine("Time to assign cells: " + sec);
        }

        /// <summary>
        /// Returns Range of Z for this set of points
        /// </summary>
        /// <returns></returns>
        public static float getZRange(HashSet<EDVector3D> ps)
        {
            return ps.Max(point => point.Z) - ps.Min(point => point.Z);
        }
    }
}
