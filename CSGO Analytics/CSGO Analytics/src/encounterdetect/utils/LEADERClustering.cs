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
        public float delta { get; set; }

        private List<Cluster> clusters = new List<Cluster>();

        private List<EDVector3D> leaders = new List<EDVector3D>();

        public LEADERClustering(List<EDVector3D> pos)
        {
            //var sorted_pos = pos.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
            var sorted_pos = pos.OrderBy(p => p.Y).ThenBy(p => p.X).ToList();
            clusterData(sorted_pos);
        }

        public LEADERClustering(float delta)
        {
            this.delta = delta;
        }

        /// <summary>
        /// Probleme: Reihenfolge der Punkte
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public Cluster[] clusterData(List<EDVector3D> pos)
        {
            clusters.Clear();
            Cluster start_cluster = new Cluster(pos[0]);
            int leader_ind = 0;
            leaders.Add(pos.First());

            clusters.Add(start_cluster);

            for (int i = 1; i < pos.Count; i++)
            {
                var datapoint = pos[i];

                var min_dist = 0.0;
                var min_dist_leader_index = 0;
                for (int index = 0; index < leaders.Count; index++)
                {
                    var leader_distance = EDMathLibrary.getEuclidDistance2D(leaders[index], datapoint);
                    if (index == 0) {
                        min_dist = leader_distance;
                        continue;
                    }
                    if (leader_distance < min_dist)
                    {
                        min_dist = leader_distance;
                        min_dist_leader_index = index;
                    }
                }

                if (min_dist < delta)
                {
                    clusters[min_dist_leader_index].AddPosition(datapoint);
                }
                else
                {
                    Cluster new_cluster = new Cluster(datapoint);
                    clusters.Add(new_cluster);
                    leader_ind++;
                    leaders.Add(datapoint);
                }
            }
            leaders.Clear();
            return clusters.ToArray();
        }

    }
}
