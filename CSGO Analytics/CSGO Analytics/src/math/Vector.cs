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

        public Vector() { }

        public Vector(float nx, float ny, float nz)
        {
            x = nx;
            y = ny;
            z = nz;
        }
        public float getY()
        {
            return y;
        }

        public float getXZ()
        {
            return z;
        }

        public float getX()
        {
            return x;
        }
        /// <summary>
        /// Initalizes a vector with an array of floats: x = arr[0], y = arr[1], z = arr[2]
        /// </summary>
        /// <param name="arr"></param>
        public Vector(float[] arr)
        {
            x = arr[0];
            y = arr[1];
            z = arr[2];
        }

        public Vector Copy()
        {
            return new Vector(x, y, z);
        }

        public double Angle2D()
        {

            return Math.Atan2(this.y, this.x);

        }

        public double Absolute()
        {

            return Math.Sqrt(AbsoluteSquared());

        }

        public Vector Normalize()
        {

            return new Vector((float)(x/Absolute()), (float)(y /Absolute()), (float)(z /Absolute()));

        }

        public double AbsoluteSquared()

        {
            return this.x * this.x + this.y * this.y + this.z * this.z;

        }

        public float[] getAsArray()
        {
            return new float[] { x, y, z };
        }

        public override string ToString()
        {
            return "x: " +x + " y: "+y +" z: "+z;
        }

        public override bool Equals(object obj)
        {
            Vector v = obj as Vector;
            return x == v.x && y == v.y && z == v.z;
        }
    }
}
