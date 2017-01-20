using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using QuadTrees.QTreePoint;

namespace CSGO_Analytics.src.math
{
    public struct EDVector3D : IComparable, IComparable<EDVector3D>, IEquatable<EDVector3D>, IPointQuadStorable
    {
        public float x, y, z;

        public System.Drawing.Point Point
        {
            get
            {
                return new System.Drawing.Point((int)x, (int)y);
            }
        }

        public EDVector3D(float nx, float ny, float nz)
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
        public EDVector3D(float[] arr)
        {
            x = arr[0];
            y = arr[1];
            z = arr[2];
        }

        public EDVector3D Copy()
        {
            return new EDVector3D(x, y, z);
        }


        public double Absolute()
        {

            return Math.Sqrt(AbsoluteSquared());

        }

        public double AbsoluteSquared()

        {
            return this.x * this.x + this.y * this.y + this.z * this.z;

        }

        public float[] getAsArray3D()
        {
            return new float[] { x, y, z };
        }

        public double[] getAsDoubleArray2D()
        {
            return new double[] { x, y };
        }

        public override string ToString()
        {
            return "x: " + x + " y: " + y + " z: " + z;
        }

        public int CompareTo(object other)
        {
            if (other is EDVector3D)
                return this.CompareTo((EDVector3D)other);
            else
                throw new Exception("Not comparable");
        }

        public int CompareTo(EDVector3D other)
        {
            if (this < other)
            {
                return -1;
            }

            if (this > other)
            {
                return 1;
            }

            return 0;
        }

        public static bool operator ==(EDVector3D v1, EDVector3D v2)
        {
            return
                v1.x == v2.x &&
                v1.y == v2.y &&
                v1.z == v2.z;
        }

        public static bool operator !=(EDVector3D v1, EDVector3D v2)
        {
            return !(v1 == v2);
        }

        public static bool operator <(EDVector3D v1, EDVector3D v2)
        {
            return v1.SumComponentSqrs() < v2.SumComponentSqrs();
        }

        public static bool operator >(EDVector3D v1, EDVector3D v2)
        {
            return v1.SumComponentSqrs() > v2.SumComponentSqrs();
        }

        public static double SumComponents(EDVector3D v1)
        {
            return v1.x + v1.y + v1.z;
        }

        public static double SumComponentSqrs(EDVector3D v1)
        {
            EDVector3D v2 = SqrComponents(v1);
            return v2.SumComponents();
        }

        public double SumComponents()
        {
            return SumComponents(this);
        }

        public static EDVector3D SqrComponents(EDVector3D v1)
        {
            return new EDVector3D(
                v1.x * v1.x,
                v1.y * v1.y,
                v1.z * v1.z);
        }

        public double SumComponentSqrs()
        {
            return SumComponentSqrs(this);
        }

        public override bool Equals(object other)
        {
            if (other is EDVector3D)
            {
                EDVector3D otherVector = (EDVector3D)other;

                return otherVector.Equals(this);
            }
            else
            {
                return false;
            }
        }

        public bool Equals(EDVector3D other)
        {
            return
               this.x.Equals(other.x) &&
               this.y.Equals(other.y) &&
               this.z.Equals(other.z);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = this.x.GetHashCode();
                hashCode = (hashCode * 397) ^ this.y.GetHashCode();
                hashCode = (hashCode * 397) ^ this.z.GetHashCode();
                return hashCode;
            }
        }
    }
}
