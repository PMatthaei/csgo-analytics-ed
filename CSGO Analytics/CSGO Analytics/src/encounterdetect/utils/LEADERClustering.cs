using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.math;

namespace CSGO_Analytics.src.encounterdetect.utils
{
    class LEADERClustering
    {
        public static float DELTA = 0.0f;

        private List<Cluster> clusters;

        public LEADERClustering(List<EDVector3D> pos, float delta)
        {
            DELTA = delta;
            clusters = new List<Cluster>();
        }

        public List<Cluster> clusterData(List<EDVector3D> pos) 
        {
            foreach(var p in pos)
            {
                var cluster = getClostestCluster(p);
                if (EDMathLibrary.getEuclidDistance2D(cluster.centroid, p) < DELTA)
                    cluster.assignToCluster(p);
                else
                    clusters.Add(new Cluster(p));
            }
            return clusters;
        }

        private Cluster getClostestCluster(EDVector3D p)
        {
            return clusters.OrderByDescending(cluster => EDMathLibrary.getEuclidDistance2D(cluster.centroid, p)).First();
        }
    }
}
