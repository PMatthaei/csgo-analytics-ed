using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_Analytics.src.math
{
    public class Polygon
    {
        public List<Vector> ps { get; set; }

        public Polygon() { }

        public Polygon(Triangle t)
        {
            this.ps = t.Vertices().ToList();
        }

        public void mergeWithTriangle(Triangle t)
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
            Polygon p = obj as Polygon;
            return p.ps.SequenceEqual(ps);
        }
    }
}
