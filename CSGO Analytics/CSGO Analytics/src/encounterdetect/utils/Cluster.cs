using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.math;
using System.Windows;

namespace CSGO_Analytics.src.encounterdetect.utils

{
    class Cluster
    {
        private const int MAX_CLUSTER_WIDTH = 30;
        private const int MAX_CLUSTER_HEIGHT = 30;

        public List<Vector3D> vs;

        public Rect cluster;

        public Cluster()
        {
            vs = new List<Vector3D>();
            cluster = new Rect();
        }

        public void AddVector(Vector3D v)
        {
            vs.Add(v);
            extendCluster(v);
        }

        public Cluster extendCluster(Vector3D v)
        {
            var max_x = vs.Max(vec => vec.x);
            var max_y = vs.Max(vec => vec.y);
            var min_x = vs.Min(vec => vec.x);
            var min_y = vs.Min(vec => vec.y);

            if (cluster.X > min_x)
                cluster.X = min_x;
            if (cluster.Y > min_y)
                cluster.Y = min_y;
            if (cluster.X + cluster.Width < max_x)
                cluster.Width = max_x - cluster.X;
            if (cluster.Y + cluster.Height < max_y)
                cluster.Height = max_y - cluster.Y;

            if (cluster.Width > MAX_CLUSTER_WIDTH || cluster.Height > MAX_CLUSTER_HEIGHT)
                return splitCluster();
            else
                return null;
        }

        private Cluster splitCluster()
        {
            return null;
        }
    }
}
