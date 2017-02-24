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
using KdTree;
using KdTree.Math;

namespace CSGO_Analytics.src.data.gameobjects
{
    public class Map
    {
        /// <summary>
        /// Array holding the different maplevels ordered from lowest level (f.e. tunnels beneath the ground) to highest (2nd floor etc)
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
        public MapLevel findLevelFromPlayer(Player p)
        {
            var vz = p.velocity.VZ;
            float pz = p.position.Z;
            if (vz != 0)
                pz -= Player.PLAYERMODELL_JUMPHEIGHT; // Substract jumpheight to get real z coordinate(see process data)
            foreach (var level in maplevels)
            {
                if (pz <= level.max_z && pz >= level.min_z || (pz == level.max_z || pz == level.min_z))
                    return level;
            }

            foreach (var level in maplevels)
            {
                Console.WriteLine(level.max_z + " " + level.min_z);
            }
            throw new Exception("Could not find Level for: " + p + " " + pz);
            return null;
            // This problem occurs because: Positions where player had z-velocity had been sorted out
            // Then we built the levels. If now such a player wants to know his level but current levels dont capture
            // This position because it was sorted out -> no suitable level found
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
            //Console.WriteLine("Start level: "+starth + " Endlevel: "+endh);
            var level_diff = Math.Abs(starth - endh);
            if (level_diff == 1) return new MapLevel[] { maplevels[endh] };

            MapLevel[] clipped_levels = new MapLevel[level_diff];

            int index = 0;
            if (starth < endh) // Case: p1 is standing on a level below p2
                for (int i = starth + 1; i <= endh; i++)
                {
                    clipped_levels[index] = maplevels[i];
                    index++;
                }
            else if (starth > endh) // Case: p2 is standing on a level below p1
                for (int i = starth - 1; i >= endh; i--)
                {
                    clipped_levels[index] = maplevels[i];
                    index++;
                }
            if (clipped_levels.Count() == 0) throw new Exception("Clipped Maplevel List cannot be empty");

            foreach (var m in clipped_levels)
            {
                if (m == null) throw new Exception("Clipped Maplevel cannot be null");
                //Console.WriteLine("Clipped Level: " + m.height);
            }


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
                Console.WriteLine("Could not find next level");
                return null;
            }
        }

    }



    public class MapLevel
    {


        /// <summary>
        /// Clusters containing the points describing this level
        /// </summary>
        public HashSet<EDVector3D[]> clusters;

        /// <summary>
        /// Array holding all grid cells 
        /// </summary>
        public EDRect[][] level_grid;
        //public EDRect[][] walkable_grid;

        /// <summary>
        /// All map cells representing obstacles and walls on this level
        /// </summary>
        public KdTree<double, EDRect> cells_tree = new KdTree<double, EDRect>(2, new DoubleMath());
        public QuadTreeRect<EDRect> qtree = new QuadTreeRect<EDRect>();

        /// <summary>
        /// Height of this level on the map - > 0 lowest level 
        /// </summary>
        public int height;

        /// <summary>
        /// Min and Max z-Koordinate occuring in levelpoints
        /// </summary>
        public float min_z, max_z;

        public MapLevel(int height, float min_z, float max_z)
        {
            this.max_z = max_z;
            this.min_z = min_z;
            this.height = height;


        }

        public override string ToString()
        {
            return "Level: " + height + " From: " + min_z + " To " + max_z;
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
