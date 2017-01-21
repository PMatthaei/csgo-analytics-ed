using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CSGO_Analytics.src.math
{
    public class EDRect
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        /// <summary>
        /// Is this rect already occupied as grid cell
        /// </summary>
        public bool occupied { get; set; }


        public bool Contains(EDVector3D v)
        {
            return new Rect(X, Y, Width, Height).Contains(new Point(v.x,v.y));
        }

 
        public override string ToString()
        {
            return "x: " + X + " y: " + Y + " width: " + Width + " height: " + Height;
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
