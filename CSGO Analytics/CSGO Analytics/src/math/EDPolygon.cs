using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_Analytics.src.math
{
    public class EDPolygon
    {
        public List<EDVector3D> ps { get; set; }

        public EDPolygon() { }

        public EDPolygon(EDTriangle t)
        {
            this.ps = t.Vertices().ToList();
        }

        public void mergeWithTriangle(EDTriangle t)
        {
            this.ps.AddRange(t.Vertices());
            this.ps.Distinct();
        }

        public override string ToString()
        {
            //return string.Join(" ; ", ps);
            return "Polygonpoints: " + ps.Count;
        }

        public override bool Equals(object obj)
        {
            EDPolygon p = obj as EDPolygon;
            return p.ps.SequenceEqual(ps);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
