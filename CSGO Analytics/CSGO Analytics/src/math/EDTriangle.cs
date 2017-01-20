using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_Analytics.src.math
{
    public class EDTriangle : IEquatable<EDTriangle>
    {
        public EDVector3D NODE1;
        public EDVector3D NODE2;
        public EDVector3D NODE3;

        public EDVector3D[] Vertices()
        {
            return new EDVector3D[] { NODE1, NODE2, NODE3 };
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool Equals(EDTriangle other)
        {
            EDTriangle t = other;

            return t.NODE1 == NODE1 || t.NODE1 == NODE2 || t.NODE1 == NODE3
                && t.NODE2 == NODE2 || t.NODE1 == NODE2 || t.NODE2 == NODE3
                && t.NODE3 == NODE3 || t.NODE3 == NODE1 || t.NODE3 == NODE2;
        }
    }
}
