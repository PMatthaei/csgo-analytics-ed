using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_Analytics.src.math
{
    public class Triangle : IEquatable<Triangle>
    {
        public Vector NODE1;
        public Vector NODE2;
        public Vector NODE3;

        public Vector[] Vertices()
        {
            return new Vector[] { NODE1, NODE2, NODE3 };
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool Equals(Triangle other)
        {
            Triangle t = other;

            return t.NODE1 == NODE1 || t.NODE1 == NODE2 || t.NODE1 == NODE3
                && t.NODE2 == NODE2 || t.NODE1 == NODE2 || t.NODE2 == NODE3
                && t.NODE3 == NODE3 || t.NODE3 == NODE1 || t.NODE3 == NODE2;
        }
    }
}
