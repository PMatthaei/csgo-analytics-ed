﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using QuadTrees.QTreePoint;
using FastDBScan;
using KdTree;

namespace CSGO_Analytics.src.math
{

    public class EDVector3D :  IComparable /*, IComparable<EDVector3D>*/, IEquatable<EDVector3D>, IPointQuadStorable
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public EDVector3D(){}

        public EDVector3D(float nx, float ny, float nz)
        {
            X = nx;
            Y = ny;
            Z = nz;
        }
        public float getY()
        {
            return Y;
        }

        public float getXZ()
        {
            return Z;
        }

        public float getX()
        {
            return X;
        }


        /// <summary>
        /// Initalizes a vector with an array of floats: x = arr[0], y = arr[1], z = arr[2]
        /// </summary>
        /// <param name="arr"></param>
        public EDVector3D(float[] arr)
        {
            X = arr[0];
            Y = arr[1];
            Z = arr[2];
        }
        public EDVector3D(double[] arr)
        {
            X = (float)arr[0];
            Y = (float)arr[1];
            Z = 0;
        }

        public EDVector3D ResetZ()
        {
            return new EDVector3D(X,Y,0);
        }

        public EDVector3D Copy()
        {
            return new EDVector3D(X, Y, Z);
        }


        public double Absolute()
        {

            return Math.Sqrt(AbsoluteSquared());

        }

        public double AbsoluteSquared()

        {
            return this.X * this.X + this.Y * this.Y + this.Z * this.Z;

        }

        public float[] getAsArray3D()
        {
            return new float[] { X, Y, Z };
        }

        public double[] getAsDoubleArray2D()
        {
            return new double[] { X, Y };
        }

        public override string ToString()
        {
            return "x: " + X + " y: " + Y + " z: " + Z;
        }

        public int CompareTo(object other)
        {
            if (other is EDVector3D)
                return this.CompareTo((EDVector3D)other);
            else
                throw new Exception("Not comparable");
        }

        /*public int CompareTo(EDVector3D other)
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
        }*/

        public static EDVector3D operator +(EDVector3D v, EDVector3D w)
        {
            return new EDVector3D(v.X + w.X, v.Y + w.Y, v.Z + w.Z);
        }

        public static EDVector3D operator -(EDVector3D v, EDVector3D w)
        {
            return new EDVector3D(v.X - w.X, v.Y - w.Y, v.Z - w.Z);
        }

        public static EDVector3D operator *(float factor, EDVector3D v)
        {
            return new EDVector3D(v.X * factor, v.Y * factor, v.Z * factor);
        }

       /* public static bool operator ==(EDVector3D v1, EDVector3D v2)
        {
            return
                v1.X == v2.X &&
                v1.Y == v2.Y &&
                v1.Z == v2.Z;
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
        }*/

        public static double SumComponents(EDVector3D v1)
        {
            return v1.X + v1.Y + v1.Z;
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
                v1.X * v1.X,
                v1.Y * v1.Y,
                v1.Z * v1.Z);
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

        public System.Drawing.Point Point
        {
            get
            {
                return new System.Drawing.Point((int)X, (int)Y);
            }
        }

        public bool Equals(EDVector3D other)
        {
            return
               this.X.Equals(other.X) &&
               this.Y.Equals(other.Y) &&
               this.Z.Equals(other.Z);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = this.X.GetHashCode();
                hashCode = (hashCode * 397) ^ this.Y.GetHashCode();
                hashCode = (hashCode * 397) ^ this.Z.GetHashCode();
                return hashCode;
            }
        }

        internal EDVector3D ChangeZ(float v)
        {
            return new EDVector3D(X,Y,Z+v);
        }
    }

    public class EDVector3DMath : ITypeMath<EDVector3D>
    {
        public EDVector3D MaxValue
        {
            get
            {
                return new EDVector3D(float.MaxValue, float.MaxValue,float.MaxValue);
            }
        }

        public EDVector3D MinValue
        {
            get
            {
                return new EDVector3D(float.MinValue, float.MinValue, float.MinValue);
            }
        }

        public EDVector3D NegativeInfinity
        {
            get
            {
                return new EDVector3D(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            }
        }

        public EDVector3D PositiveInfinity
        {
            get
            {
                return new EDVector3D(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            }
        }

        public EDVector3D Zero
        {
            get
            {
                return new EDVector3D(0, 0,0);
            }
        }

        public EDVector3D Add(EDVector3D a, EDVector3D b)
        {
            return a + b;
        }

        public bool AreEqual(EDVector3D[] a, EDVector3D[] b)
        {
            throw new NotImplementedException();
        }

        public bool AreEqual(EDVector3D a, EDVector3D b)
        {
            throw new NotImplementedException();
        }

        public int Compare(EDVector3D a, EDVector3D b)
        {
            throw new NotImplementedException();
        }

        public EDVector3D DistanceSquaredBetweenPoints(EDVector3D[] a, EDVector3D[] b)
        {
            throw new NotImplementedException();
        }

        public EDVector3D Max(EDVector3D a, EDVector3D b)
        {
            if (a.Absolute() > b.Absolute())
                return a;
            else
                return b;
        }

        public EDVector3D Min(EDVector3D a, EDVector3D b)
        {
            if (a.Absolute() < b.Absolute())
                return a;
            else
                return b;
        }

        public EDVector3D Multiply(EDVector3D a, EDVector3D b)
        {
            return new EDVector3D(a.X * b.X, a.Y*b.Y, a.Z*b.Z);
        }

        public EDVector3D Subtract(EDVector3D a, EDVector3D b)
        {
            return a-b;
      }
    }
}
