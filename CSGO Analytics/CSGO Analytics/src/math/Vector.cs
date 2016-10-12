using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_Analytics.src.math
{
    public class Vector
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }

        public double Angle2D
        {
            get
            {
                return Math.Atan2(this.y, this.x);
            }
        }

        public double Absolute
        {
            get
            {
                return Math.Sqrt(AbsoluteSquared);
            }
        }

        public double AbsoluteSquared
        {
            get
            {
                return this.x * this.x + this.y * this.y + this.z * this.z;
            }
        }

        public float[] getAsArray()
        {
            return new float[]{x,y,z};
        }
    }
}
