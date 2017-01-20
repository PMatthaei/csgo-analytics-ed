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
    public class EDRect : IQuadObject
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        /// <summary>
        /// Is this rect already occupied as grid cell
        /// </summary>
        public bool occupied { get; set; }

        public Rect Bounds
        {
            get
            {
                return new Rect(X, Y, Width, Height);
            }
        }

        public bool Contains(EDVector3D v)
        {
            return new Rect(X, Y, Width, Height).Contains(new Point(v.x,v.y));
        }

        public event EventHandler BoundsChanged;

        private void RaiseBoundsChanged()
        {
            EventHandler handler = BoundsChanged;
            if (handler != null)
                handler(this, new EventArgs());
        }

        public override string ToString()
        {
            return "x: " + X + " y: " + Y + " width: " + Width + " height: " + Height;
        }

        public System.Drawing.Rectangle getQuadRect()
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
