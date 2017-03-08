using System;
using System.Collections.Generic;
using System.Linq;
using CSGO_Analytics.src.math;
using QuadTrees;
using KdTree;
using KdTree.Math;

namespace CSGO_Analytics.src.data.gameobjects
{
    public class Map
    {
        public static string[] SUPPORTED_MAPS = new string[] { "de_dust2" , "de_cbble","de_cache","de_mirage","de_inferno", "de_overpass" };

        /// <summary>
        /// Array holding the different maplevels ordered from lowest level (f.e. tunnels beneath the ground) to highest (2nd floor etc)
        /// </summary>
        public MapLevel[] maplevels;

        /// <summary>
        /// All obstacles of a map which are dynamic in their appearance and/or position
        /// </summary>
        public HashSet<MapObstacle> dynamic_obstacles;

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
            this.dynamic_obstacles = new HashSet<MapObstacle>();
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


        /// <summary>
        /// 
        /// </summary>
        /// <param name="ml_height"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
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
        public MapgridCell[][] level_grid;

        /// <summary>
        /// All map cells representing obstacles and walls on this level - maybe use kdtree for nearest neighbors
        /// </summary>
        public KdTree<double, MapgridCell> cells_tree = new KdTree<double, MapgridCell>(2, new DoubleMath());

        /// <summary>
        /// All cells representing walls in a quadtree
        /// </summary>
        public QuadTreeRect<MapgridCell> walls_tree = new QuadTreeRect<MapgridCell>();

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

    public class MapgridCell : EDRect
    {
        /// <summary>
        /// Index x of the mapcell in the map grid
        /// </summary>
        public int index_X { get; set; }

        /// <summary>
        /// Index y of the mapcell in the map grid
        /// </summary>
        public int index_Y { get; set; }

        /// <summary>
        /// Is this rect already occupied as grid cell -> it has not been walked by a player
        /// </summary>
        public bool blocked { get; set; }

        public MapgridCell Copy()
        {
            return new MapgridCell
            {
                index_X = index_X,
                index_Y = index_Y,
                X = X,
                Y = Y,
                Width = Width,
                Height = Height,
                blocked = false
            };
        }

        public override string ToString()
        {
            return "x: " + X + " y: " + Y + " width: " + Width + " height: " + Height + " index x: " + index_X + " index y: " + index_Y;
        }
    }

    public class MapObstacle
    {

    }


}
