using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_Analytics.src.math
{
    public class Vector3D
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }

        public Vector3D() { }

        public Vector3D(float nx, float ny, float nz)
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
        public Vector3D(float[] arr)
        {
            x = arr[0];
            y = arr[1];
            z = arr[2];
        }

        public Vector3D Copy()
        {
            return new Vector3D(x, y, z);
        }

        public double Angle2D()
        {

            return Math.Atan2(this.y, this.x);

        }

        public double Absolute()
        {

            return Math.Sqrt(AbsoluteSquared());

        }

        public Vector3D Normalize()
        {

            return new Vector3D((float)(x/Absolute()), (float)(y /Absolute()), (float)(z /Absolute()));

        }

        public double AbsoluteSquared()

        {
            return this.x * this.x + this.y * this.y + this.z * this.z;

        }

        public float[] getAsArray()
        {
            return new float[] { x, y, z };
        }

        public double[] getAsDoubleArray()
        {
            return new double[] { x, y };
        }

        public override string ToString()
        {
            return "x: " +x + " y: "+y +" z: "+z;
        }

        public override bool Equals(object obj)
        {
            Vector3D v = obj as Vector3D;
            return x == v.x && y == v.y && z == v.z;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
