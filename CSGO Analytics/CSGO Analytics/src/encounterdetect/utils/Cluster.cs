using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.math;
using System.Collections;

/// <summary>
/// TODO: make generic some time
/// </summary>
namespace CSGO_Analytics.src.encounterdetect.utils
{
    public class Cluster
    {
        public List<EDVector3D> data;

        public EDVector3D centroid;

        public double cluster_attackrange { get; set; }


        public Cluster()
        {
            this.data = new List<EDVector3D>();

        }

        public Cluster(EDVector3D[] data)
        {
            this.data = data.ToList();
        }

        public Cluster(EDVector3D datapoint)
        {
            this.data = new List<EDVector3D>();
            assignToCluster(datapoint);
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
            Console.WriteLine("Attackrange for this cluster is: " + cluster_attackrange);
        }

        internal void assignToCluster(EDVector3D p)
        {
            data.Add(p);
        }

        public EDRect getBoundings()
        {
            var min_x = data.Min(point => point.X);
            var min_y = data.Min(point => point.Y);
            var max_x = data.Max(point => point.X);
            var max_y = data.Max(point => point.Y);
            var dx = max_x - min_x;
            var dy = max_y - min_y;
            return new EDRect { X = min_x, Y = max_y, Width = dx, Height = dy };
        }


        public void AddPosition(EDVector3D p)
        {
            data.Add(p);
        }
    }
}
