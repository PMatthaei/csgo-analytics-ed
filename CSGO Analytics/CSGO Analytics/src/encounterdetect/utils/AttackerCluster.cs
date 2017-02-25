using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using CSGO_Analytics.src.math;


namespace CSGO_Analytics.src.encounterdetect.utils
{
    public class AttackerCluster : Cluster
    {
        public double cluster_attackrange { get; set; }

        public double max_attackrange { get; set; }

        public double min_attackrange { get; set; }

        public AttackerCluster(EDVector3D[] data) : base(data)
        {
        }

        internal void calculateClusterAttackrange(Hashtable ht)
        {
            double[] distances = new double[data.Count];
            int arr_ptr = 0;
            if (data.Count == 0) return;
            foreach (var pos in data)
            {
                EDVector3D value = (EDVector3D)ht[pos]; // No Z variable no hashtable entry -> null -.-
                distances[arr_ptr] = EDMathLibrary.getEuclidDistance2D(pos, value);
                arr_ptr++;
            }

            cluster_attackrange = distances.Average();
            max_attackrange = distances.Max();
            min_attackrange = distances.Min();
            Console.WriteLine("Attackrange for this cluster is: " + cluster_attackrange);
        }
    }
}
