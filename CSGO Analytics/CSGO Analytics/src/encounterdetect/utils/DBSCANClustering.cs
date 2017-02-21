using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FastDBScan;
using KDTree;
using CSGO_Analytics.src.math;


namespace FastDBScan
{
    /// <summary>
    /// Main Codeparts from http://codereview.stackexchange.com/questions/108965/implementing-a-fast-dbscan-in-c
    /// </summary>
    public class KD_DBSCANClustering
    {
        private readonly Func<EDVector3D, EDVector3D, double> _metricFunc;

        public KD_DBSCANClustering(Func<EDVector3D, EDVector3D, double> metricFunc)
        {
            _metricFunc = metricFunc;
        }

        public HashSet<EDVector3D[]> ComputeClusterDbscan(EDVector3D[] allPoints, double epsilon, int minPts)
        {
            var allPointsDbscan = allPoints.Select(x => new DbscanPoint(x)).ToArray();

            var tree = new KDTree.KDTree<DbscanPoint>(2);
            for (var i = 0; i < allPointsDbscan.Length; ++i)
            {
                tree.AddPoint(new double[] { allPointsDbscan[i].ClusterPoint.X, allPointsDbscan[i].ClusterPoint.Y }, allPointsDbscan[i]);
            }

            var C = 0;
            for (int i = 0; i < allPointsDbscan.Length; i++)
            {
                var p = allPointsDbscan[i];
                if (p.IsVisited)
                    continue;
                p.IsVisited = true;

                DbscanPoint[] neighborPts = RegionQuery(tree, p.ClusterPoint, epsilon);
                if (neighborPts.Length < minPts)
                    p.ClusterId = (int)ClusterIDs.Noise;
                else
                {
                    C++;
                    ExpandCluster(tree, p, neighborPts, C, epsilon, minPts);
                }
            }
            return new HashSet<EDVector3D[]>(
                allPointsDbscan
                    .Where(x => x.ClusterId > 0)
                    .GroupBy(x => x.ClusterId)
                    .Select(x => x.Select(y => y.ClusterPoint).ToArray())
                );
        }

        private static void ExpandCluster(KDTree<DbscanPoint> tree, DbscanPoint p, DbscanPoint[] neighborPts, int c, double epsilon, int minPts)
        {
            p.ClusterId = c;

            var queue = new Queue<DbscanPoint>(neighborPts);
            while (queue.Count > 0)
            {
                var point = queue.Dequeue();
                if (point.ClusterId == (int)ClusterIDs.Unclassified)
                {
                    point.ClusterId = c;
                }

                if (point.IsVisited)
                {
                    continue;
                }

                point.IsVisited = true;
                var neighbors = RegionQuery(tree, point.ClusterPoint, epsilon);
                if (neighbors.Length >= minPts)
                {
                    foreach (var neighbor in neighbors.Where(neighbor => !neighbor.IsVisited))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        private static DbscanPoint[] RegionQuery(KDTree<DbscanPoint> tree, EDVector3D p, double epsilon)
        {
            var neighbors = new List<DbscanPoint>();
            var e = tree.NearestNeighbors(p.getAsDoubleArray2D(), 10, epsilon);
            while (e.MoveNext())
            {
                neighbors.Add(e.GetEnumerator().Current);
            }

            return neighbors.ToArray();
        }
    }

    //EDVector3D container for Dbscan clustering
    public class DbscanPoint
    {
        public bool IsVisited;
        public EDVector3D ClusterPoint;
        public int ClusterId;

        public DbscanPoint(EDVector3D point)
        {
            ClusterPoint = point;
            IsVisited = false;
            ClusterId = (int)ClusterIDs.Unclassified;
        }
    }

    public enum ClusterIDs
    {
        Unclassified = 0,
        Noise = -1
    }


}
