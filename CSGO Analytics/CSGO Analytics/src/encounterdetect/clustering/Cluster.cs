using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.math;

namespace CSGO_Analytics.src.encounterdetect.clustering
{
    class Cluster
    {
        public List<Vector> vs;

        public Rectangle cluster;

        public void AddVector(Vector v)
        {
            vs.Add(v);
            extendCluster(v);
        }

        public void extendCluster(Vector v)
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

        }
    }
}
