using QuadTrees.QTreeRect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Drawing;

namespace CSGO_Analytics.src.math
{
    public class EDRect : IRectQuadStorable
    {
        public double X { get; set; }
        public double Y { get; set; }
        public int index_X { get; set; }
        public int index_Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        /// <summary>
        /// Is this rect already occupied as grid cell -> it has not been walked by a player
        /// </summary>
        public bool blocked { get; set; }

        public EDVector3D Center
        {
            get
            {
                return new EDVector3D((float)(X+Width/2), (float)(Y + Height / 2), 0);
            }
        }


        public System.Drawing.Rectangle Rect
        {
            get
            {
                return getAsQuadTreeRect();
            }
        }

        public bool Contains(EDVector3D v)
        {
            return new Rect(X, Y, Width, Height).Contains(new System.Windows.Point(v.X, v.Y));
        }

        public EDRect Copy()
        {
            return new  EDRect
                    {
                        index_X = index_X,
                        index_Y = index_Y,
                        X = X,
                        Y = Y,
                        Width = Width,
                        Height = Height,
                        blocked = false
                    };
        }

        public override string ToString()
        {
            return "x: " + X + " y: " + Y + " width: " + Width + " height: " + Height+ " index x: "+index_X + " index y: "+index_Y;
        }

        public override bool Equals(object obj)
        {
            EDRect r = obj as EDRect;

            return r.X == X && r.Y == Y && r.Width == Width && r.Height == Height;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public System.Windows.Rect getAsComputeRect()
        {
            return new System.Windows.Rect
            {
                X = X,
                Y = Y,
                Width = Width,
                Height = Height
            };
        }

        public System.Drawing.Rectangle getAsQuadTreeRect()
        {
            return new System.Drawing.Rectangle
            {
                X = (int)X,
                Y = (int)Y,
                Width = (int)Width,
                Height = (int)Height
            };
        }
    }
}
