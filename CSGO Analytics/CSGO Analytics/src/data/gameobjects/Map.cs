﻿using System;
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
                var pz = p.position.Z;
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
        internal MapLevel[] getClippedLevels(int starth, int endh)
        {
            var length = Math.Abs(starth - endh) - 1;
            MapLevel[] clipped_levels = new MapLevel[length];

            int index = 0;
            if (starth < endh) // Case: p1 is standing on a level below p2
                for (int i = starth + 1; i <= endh; i++)
                {
                    clipped_levels[index] = maplevels[i];
                    index++;
                }
            else if (endh < starth) // Case: p2 is standing on a level below p1
                for (int i = starth - 1; i > endh; i--)
                {
                    clipped_levels[index] = maplevels[i];
                    index++;
                }

            #region Deprecated
            /*
            var levelamount = maplevels.Length;
            var aim = EDMathLibrary.getAimVector(player.position, player.facing);
            var maxclips = (levelamount - player_maplevelheight) - 1;
            int[] clipped_levels = new int[maxclips];

            //if (aim.z > player.position.z) 
            if (0 < player.facing.pitch) //nach oben aimen
                    for (int i = 0; i < maxclips; i++)
                    clipped_levels[i] = player_maplevelheight + i;
            //if (aim.z < player.position.z) // nach unten aimen
            if (0 > player.facing.pitch)
                    for (int i = 0; i < maxclips; i++)
                    clipped_levels[i] = player_maplevelheight - i;
                    */
            #endregion
            return clipped_levels;
        }

        public MapLevel nextLevel(int ml_height, int dir)
        {
            if (dir != 1 || dir != -1) throw new Exception("Direction not matching");
            try
            {
                return maplevels[ml_height + dir];
            }
            catch (Exception e)
            {
                return null;
            }

        }
        public List<MapLevel> getMapLevelNeighbors(int ml_height)
        {
            var mls = new List<MapLevel>();
            var upperIndex = ml_height + 1;
            var lowerIndex = ml_height - 1;
            try
            {
                mls.Add(maplevels[upperIndex]);
            }
            catch (Exception e) { }
            try
            {
                mls.Add(maplevels[lowerIndex]);
            }
            catch (Exception e) { }
            return mls;
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

        private static HashSet<EDRect> map_grid;

        private const int cellwidth = 50;

        /// <summary>
        /// This function takes a list of all registered points on the map and tries to
        /// reconstruct a polygonal represenatation of the map with serveral levels
        /// </summary>
        /// <param name="ps"></param>
        public static Map createMap(HashSet<EDVector3D> ps)
        {
            // Deploy a grid over the map
            var count = 0;
            var currentx = pos_x;
            var currenty = pos_y;
            map_grid = new HashSet<EDRect>();
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
            var map_width_x = ps.Max(point => point.X) - ps.Min(point => point.X);
            var map_width_y = ps.Max(point => point.Y) - ps.Min(point => point.Y);
            Console.WriteLine("Max x: " + ps.Max(point => point.X) + " Min x: " + ps.Min(point => point.X));
            Console.WriteLine("Mapwidth in x-Range: " + map_width_x + " Mapwidth in y-Range: " + map_width_y);

            return new Map(map_width_x, map_width_y, maplevels);
        }

        private static MapLevel[] createMapLevels(HashSet<EDVector3D> ps)
        {
            MapLevel[] maplevels;
            int levelamount = (int)Math.Ceiling((getZRange(ps) / LEVELHEIGHT));
            maplevels = new MapLevel[levelamount];

            Console.WriteLine("Levels to create: " + levelamount);
            var min_z = ps.Min(point => point.Z);
            var max_z = ps.Max(point => point.Z);
            //Console.WriteLine("From Min Z: " + min_z + " to Max Z: " + max_z);

            for (int i = 0; i < levelamount; i++)
            {
                var upperbound = min_z + (i + 1) * LEVELHEIGHT;
                var lowerbound = min_z + i * LEVELHEIGHT;
                var levelps = new HashSet<EDVector3D>(ps.Where(point => point.Z >= lowerbound && point.Z <= upperbound).OrderBy(point => point.Z));
                // Console.WriteLine("Z Range for Level " + i + " between " + lowerbound + " and " + upperbound);

                if (levelps.Count() == 0)
                {
                    Console.WriteLine("No points on level:" + i);
                    continue;
                }

                Console.WriteLine("Level " + i + ": " + levelps.Count() + " points");
                //Console.WriteLine("Level " + i + " starts: " + levelps.First().ToString());
                //Console.WriteLine("Level " + i + " ends: " + levelps.Last().ToString());
                var ml = new MapLevel(levelps, i);
                ml.assignLevelcells(map_grid);
                maplevels[i] = ml;
            }

            return maplevels;
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

    public class MapLevel
    {
        private const int MIN_CELL_QUERY = 1;

        /// <summary>
        /// Points describing this level
        /// </summary>
        public HashSet<EDVector3D> ps;

        /// <summary>
        /// Height of this level on the map - > 0 lowest level 
        /// </summary>
        public int height;

        /// <summary>
        /// All map cells representing the free, walkable space of this level
        /// </summary>
        public HashSet<EDRect> level_cells = new HashSet<EDRect>();

        /// <summary>
        /// All map cells representing obstacles and walls on this level
        /// </summary>
        public QuadTreeRect<EDRect> qlevel_walls = new QuadTreeRect<EDRect>();

        /// <summary>
        /// Min and Max z-Koordinate occuring in levelpoints
        /// </summary>
        public float min_z, max_z;

        public MapLevel(HashSet<EDVector3D> nps, int height)
        {
            this.ps = nps;
            this.max_z = ps.Max(point => point.Z);
            this.min_z = ps.Min(point => point.Z);

            this.height = height;
        }

        public void assignLevelcells(HashSet<EDRect> map_grid)
        {
            #region QuadTree Approach
            var watch = System.Diagnostics.Stopwatch.StartNew();

            QuadTreePoint<EDVector3D> qtree = new QuadTreePoint<EDVector3D>();
            removeOutliers(ps).Distinct().ToList().ForEach(v => qtree.Add(v));
            foreach (var cell in map_grid)
            {
                var rectps = qtree.GetObjects(cell.getAsQuadTreeRect()); //Get points in a cell
                if (rectps.Count >= MIN_CELL_QUERY)
                    level_cells.Add(cell);
            }

            qlevel_walls.AddRange(map_grid.Except(level_cells).OrderBy(r => r.X).ThenBy(r => r.Y));

            // TODO: Solve maximal rectangle problem
            // TODO: Fill holes cells with more than 2 or 3 neighbors -> prevent obstacles which are not there just because nobody has walked at this cell
            // TODO: Remove outliers of pointclouds
            Console.WriteLine("Occupied cells by this level: " + level_cells.Count);
            watch.Stop();
            var sec = watch.ElapsedMilliseconds / 1000.0f;
            Console.WriteLine("Time to assign cells: " + sec);
            #endregion
        }

        private HashSet<EDVector3D> removeOutliers(HashSet<EDVector3D> ps)
        {
            return ps;
        }

        public List<EDRect> getCellNeighbors(EDRect cell)
        {
            var neighbors = qlevel_walls.GetObjects(new System.Drawing.Rectangle
            {
                X = (int)(cell.X - cell.Width),
                Y = (int)(cell.Y - cell.Height),
                Width = (int)(3 * cell.Width),
                Height = (int)(3 * cell.Height),
            });
            return neighbors;
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
