using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace CSGO_Analytics.src.math
{
    public class GrahamScan
    {
        const int TURN_LEFT = 1;
        const int TURN_RIGHT = -1;
        const int TURN_NONE = 0;

        public static int turn(Vector p, Vector q, Vector r)
        {
            return ((q.getX() - p.getX()) * (r.getY() - p.getY()) - (r.getX() - p.getX()) * (q.getY() - p.getY())).CompareTo(0);
        }

        public static void keepLeft(List<Vector> hull, Vector r)
        {
            while (hull.Count > 1 && turn(hull[hull.Count - 2], hull[hull.Count - 1], r) != TURN_LEFT)
            {
                hull.RemoveAt(hull.Count - 1);
            }
            if (hull.Count == 0 || hull[hull.Count - 1] != r)
            {
                hull.Add(r);
            }

        }

        public static double getAngle(Vector p1, Vector p2)
        {
            float xDiff = p2.getX() - p1.getX();
            float yDiff = p2.getY() - p1.getY();
            return Math.Atan2(yDiff, xDiff) * 180.0 / Math.PI;
        }

        public static List<Vector> MergeSort(Vector p0, List<Vector> arrPoint)
        {
            if (arrPoint.Count == 1)
            {
                return arrPoint;
            }
            List<Vector> arrSortedInt = new List<Vector>();
            int middle = (int)arrPoint.Count / 2;
            List<Vector> leftArray = arrPoint.GetRange(0, middle);
            List<Vector> rightArray = arrPoint.GetRange(middle, arrPoint.Count - middle);
            leftArray = MergeSort(p0, leftArray);
            rightArray = MergeSort(p0, rightArray);
            int leftptr = 0;
            int rightptr = 0;
            for (int i = 0; i < leftArray.Count + rightArray.Count; i++)
            {
                if (leftptr == leftArray.Count)
                {
                    arrSortedInt.Add(rightArray[rightptr]);
                    rightptr++;
                }
                else if (rightptr == rightArray.Count)
                {
                    arrSortedInt.Add(leftArray[leftptr]);
                    leftptr++;
                }
                else if (getAngle(p0, leftArray[leftptr]) < getAngle(p0, rightArray[rightptr]))
                {
                    arrSortedInt.Add(leftArray[leftptr]);
                    leftptr++;
                }
                else
                {
                    arrSortedInt.Add(rightArray[rightptr]);
                    rightptr++;
                }
            }
            return arrSortedInt;
        }

        public static List<Vector> convexHull(List<Vector> points)
        {

            Vector p0 = null;
            foreach (Vector value in points)
            {
                if (p0 == null)
                    p0 = value;
                else
                {
                    if (p0.getY() > value.getY())
                        p0 = value;
                }
            }
            List<Vector> order = new List<Vector>();
            foreach (Vector value in points)
            {
                if (p0 != value)
                    order.Add(value);
            }

            order = MergeSort(p0, order);

            List<Vector> result = new List<Vector>();
            result.Add(p0);
            result.Add(order[0]);
            result.Add(order[1]);
            order.RemoveAt(0);
            order.RemoveAt(0);

            foreach (Vector value in order)
            {
                keepLeft(result, value);
            }

            return result;
        }

    }

}