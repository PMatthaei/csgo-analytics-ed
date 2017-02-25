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
