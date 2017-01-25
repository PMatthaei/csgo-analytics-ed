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
        public double Width { get; set; }
        public double Height { get; set; }

        /// <summary>
        /// Is this rect already occupied as grid cell
        /// </summary>
        public bool occupied { get; set; }

        public System.Drawing.Rectangle Rect
        {
            get
            {
                return getAsQuadTreeRect();
            }
        }

        public bool Contains(EDVector3D v)
        {
            return new Rect(X, Y, Width, Height).Contains(new System.Windows.Point(v.X,v.Y));
        }

 
        public override string ToString()
        {
            return "x: " + X + " y: " + Y + " width: " + Width + " height: " + Height;
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
            return new System.Drawing.Rectangle {
                X = (int)X,
                Y = (int)Y,
                Width = (int)Width,
                Height = (int)Height
            };
        }
    }
}
