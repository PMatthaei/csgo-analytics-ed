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

namespace CSGO_Analytics.src.data.gameobjects
{
    public class Map
    {
        /// <summary>
        /// Array holding the different maplevels ordered from lowest level (tunnels beneth the ground) to highest ( 2nd floor etc)
        /// </summary>
        public MapLevel[] maplevels;

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
        /// Returns if this player is standing on the level
        /// </summary>
        /// <returns></returns>
        public MapLevel findPlayerLevel(Player p)
        {
            foreach (var level in maplevels)
            {
                var ps = level.ps;
                var pz = p.position.z;
                if (pz <= level.max_z && pz >= level.min_z)
                    return level;
            }
            return null;

        }
        /// <summary>
        /// Returns a bounding box of the map with root at 0,0
        /// </summary>
        /// <returns></returns>
        public EDRect getMapBoundingBox()
        {
            return new EDRect
            {
                X = 0,
                Y = 0,
                Width = this.width_x,
                Height = this.width_y
            };
        }

        /// <summary>
        /// Returns arry of indices maplevel of all maplevels this players aimvector has clipped
        /// </summary>
        /// <param name="player"></param>
        /// <param name="player_maplevel"></param>
        /// <returns></returns>
        internal int[] getClippedLevels(Player player, int player_maplevelheight)
        {
            var levelamount = maplevels.Length;
            var aim = EDMathLibrary.getAimVector(player.position, player.facing);
            var clips = levelamount - player_maplevelheight;
            int[] clipped_levels = new int[clips];

            if (aim.z > player.position.z)
                for (int i = 0; i < clips; i++)
                    clipped_levels[i] = player_maplevelheight + i;
            if (aim.z < player.position.z)
                for (int i = 0; i < clips; i++)
                    clipped_levels[i] = player_maplevelheight - i;

            return clipped_levels;
        }
    }

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

        private static List<EDRect> map_grid;

        private const int cellwidth = 50;

        /// <summary>
        /// This function takes a list of all registered points on the map and tries to
        /// reconstruct a polygonal represenatation of the map with serveral levels
        /// </summary>
        /// <param name="ps"></param>
        public static Map createMap(List<EDVector3D> ps)
        {
            // Deploy a grid over the map
            var count = 0;
            var currentx = pos_x;
            var currenty = pos_y;
            map_grid = new List<EDRect>();
            var cells = (mapdata_height / cellwidth) * (mapdata_width / cellwidth);
            for (int i = 0; i < cells; i++)
            {
                map_grid.Add(new EDRect
                {
                    X = currentx,
                    Y = currenty,
                    Width = cellwidth,
                    Height = cellwidth,
                    occupied = false
                });
                count++;

                if (count % (mapdata_width / cellwidth) != 0)// new linesegment
                {
                    currentx += cellwidth;
                }
                else if (count % (mapdata_width / cellwidth) == 0) //new line
                {
                    currentx = pos_x;
                    currenty -= cellwidth;
                    count = 0;
                }
            }

            // Create the map levels 
            MapLevel[] maplevels = createMapLevels(ps);
            var map_width_x = ps.Max(point => point.x) - ps.Min(point => point.x);
            var map_width_y = ps.Max(point => point.y) - ps.Min(point => point.y);
            Console.WriteLine("Max x: " + ps.Max(point => point.x) + " Min x: " + ps.Min(point => point.x));
            Console.WriteLine("Mapwidth in x-Range: " + map_width_x + " Mapwidth in y-Range: " + map_width_y);

            return new Map(map_width_x, map_width_y, maplevels);
        }

        private static MapLevel[] createMapLevels(List<EDVector3D> ps)
        {
            MapLevel[] maplevels;
            int levelamount = (int)Math.Ceiling((getZRange(ps) / LEVELHEIGHT));
            maplevels = new MapLevel[levelamount];

            Console.WriteLine("Levels to create: " + levelamount);
            var min_z = ps.Min(point => point.z);
            var max_z = ps.Max(point => point.z);
            //Console.WriteLine("From Min Z: " + min_z + " to Max Z: " + max_z);

            for (int i = 0; i < levelamount; i++)
            {
                var upperbound = min_z + (i + 1) * LEVELHEIGHT;
                var lowerbound = min_z + i * LEVELHEIGHT;
                var levelps = ps.Where(point => point.z >= lowerbound && point.z <= upperbound).OrderBy(point => point.z);
                // Console.WriteLine("Z Range for Level " + i + " between " + lowerbound + " and " + upperbound);

                if (levelps.Count() == 0)
                {
                    Console.WriteLine("No points on level:" + i);
                    continue;
                }

                Console.WriteLine("Level " + i + ": " + levelps.Count() + " points");
                //Console.WriteLine("Level " + i + " starts: " + levelps.First().ToString());
                //Console.WriteLine("Level " + i + " ends: " + levelps.Last().ToString());
                var ml = new MapLevel(levelps.ToList(), i);
                ml.assignLevelcells(map_grid);
                maplevels[i] = ml;
            }

            return maplevels;
        }

        /// <summary>
        /// Returns Range of Z for this set of points
        /// </summary>
        /// <returns></returns>
        public static float getZRange(List<EDVector3D> ps)
        {
            return ps.Max(point => point.z) - ps.Min(point => point.z);
        }
    }

    public class MapLevel
    {
        /// <summary>
        /// Points describing this level
        /// </summary>
        public List<EDVector3D> ps;

        /// <summary>
        /// Height of this level on the map - > 0 lowest level 
        /// </summary>
        public int height; 

        /// <summary>
        /// All map cells representing the free, walkable space of this level
        /// </summary>
        public List<EDRect> level_cells = new List<EDRect>();

        /// <summary>
        /// All map cells representing obstacles and walls on this level
        /// </summary>
        public List<EDRect> level_walls = new List<EDRect>();

        public float min_z, max_z;

        public MapLevel(List<EDVector3D> nps, int height)
        {
            this.ps = nps;
            this.max_z = ps.Max(point => point.z);
            this.min_z = ps.Min(point => point.z);

            this.height = height;
        }

        public void assignLevelcells(List<EDRect> map_grid)
        {
            #region QuadTree Approach
            var watch = System.Diagnostics.Stopwatch.StartNew();

            QuadTreePoint<EDVector3D> qtree = new QuadTreePoint<EDVector3D>();
            removeOutliers(ps).Distinct().ToList().ForEach(v => qtree.Add(v));
            foreach (var cell in map_grid)
            {
                var rectps = qtree.GetObjects(cell.getAsQuadTreeRect()); //Get points in a cell
                if (rectps.Count > 0) //If at least 2 points are within the rect -> mark cell as part of the level
                    level_cells.Add(cell);
            }

            level_walls = map_grid.ToList().Except(level_cells).ToList();

            // TODO: Solve maximal rectangle problem
            // TODO: Fill holes cells with more than 2 or 3 neighbors -> prevent obstacles which are not there just because nobody has walked at this cell
            // TODO: Remove outliers of pointclouds
            Console.WriteLine("Occupied cells by this level: " + level_cells.Count);
            watch.Stop();
            var sec = watch.ElapsedMilliseconds / 1000.0f;
            Console.WriteLine("Time to assign cells: " + sec);
            #endregion
        }

        private List<EDVector3D> removeOutliers(List<EDVector3D> ps)
        {
            return ps;
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
        /// <summary>
        /// Reads a map info file "<mapname>".txt and extracts the relevant data about the map
        /// </summary>
        /// <param name="path"></param>
        public static MapMetaData readProperties(string path)
        {
            string line;

            var fmt = new NumberFormatInfo();
            fmt.NegativeSign = "-";

            MapMetaData metadata = new MapMetaData();
            using (var file = new StreamReader(path))
            {
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
            return metadata;
        }

    }
}
