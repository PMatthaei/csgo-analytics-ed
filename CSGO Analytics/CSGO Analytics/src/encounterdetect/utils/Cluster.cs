using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.math;

namespace CSGO_Analytics.src.encounterdetect.utils

{
    class Cluster
    {
        private const int MAX_CLUSTER_WIDTH = 30;
        private const int MAX_CLUSTER_HEIGHT = 30;

        public List<Vector> vs;

        public Rectangle cluster;

        public Cluster()
        {
            vs = new List<Vector>();
            cluster = new Rectangle();
        }

        public void AddVector(Vector v)
        {
            vs.Add(v);
            extendCluster(v);
        }

        public Cluster extendCluster(Vector v)
        {
            var max_x = vs.Max(vec => vec.x);
            var max_y = vs.Max(vec => vec.y);
            var min_x = vs.Min(vec => vec.x);
            var min_y = vs.Min(vec => vec.y);

            if (cluster.x > min_x)
                cluster.x = min_x;
            if (cluster.y > min_y)
                cluster.y = min_y;
            if (cluster.x + cluster.width < max_x)
                cluster.width = max_x - cluster.x;
            if (cluster.y + cluster.height < max_y)
                cluster.height = max_y - cluster.y;

            if (cluster.width > MAX_CLUSTER_WIDTH || cluster.height > MAX_CLUSTER_HEIGHT)
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
