using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows;

namespace CSGO_Analytics.src.views
{
    class PlayerShape : Shape
    {
        /// <summary>
        /// Line of sight length of a playershape
        /// </summary>
        private const int LOS_LENGTH = 40;

        private Point aimPoint;

        public bool Active { get; set; }

        public double Radius { get; set; }

        public double Yaw
        {
            get { return (double)GetValue(yawProperty); }
            set { SetValue(yawProperty, value); }
        }

        // DependencyProperty - Yaw
        private static FrameworkPropertyMetadata yawMetadata =
                new FrameworkPropertyMetadata(
                    90.0,     // Default value
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    null,    // Property changed callback
                    null);   // Coerce value callback

        public static readonly DependencyProperty yawProperty =
            DependencyProperty.Register("Yaw", typeof(double), typeof(PlayerShape), yawMetadata);


        public double X
        {
            get { return (double)GetValue(xProperty); }
            set { SetValue(xProperty, value); }
        }

        // DependencyProperty - Position X
        private static FrameworkPropertyMetadata XMetadata =
                new FrameworkPropertyMetadata(
                    0.0,     // Default value
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    null,    // Property changed callback
                    null);   // Coerce value callback

        public static readonly DependencyProperty xProperty =
            DependencyProperty.Register("X", typeof(double), typeof(PlayerShape), XMetadata);

        public double Y
        {
            get { return (double)GetValue(yProperty); }
            set { SetValue(yProperty, value); }
        }

        // DependencyProperty - Position Y
        private static FrameworkPropertyMetadata YMetadata =
                new FrameworkPropertyMetadata(
                    0.0,     // Default value
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    null,    // Property changed callback
                    null);   // Coerce value callback

        public static readonly DependencyProperty yProperty =
            DependencyProperty.Register("Y", typeof(double), typeof(PlayerShape), YMetadata);


        protected override Geometry DefiningGeometry
        {
            get
            {
                var aimX = (X + LOS_LENGTH * Math.Cos(Yaw)); // Aim vector from Yaw -> dont forget toRadian for this calc
                var aimY = (Y + LOS_LENGTH * Math.Sin(Yaw));

                if (aimPoint == null)
                    aimPoint = new Point(aimX, aimY);

                aimPoint.X = aimX;
                aimPoint.Y = aimY;
                Geometry line = new LineGeometry(new Point(X, Y), aimPoint);
                Geometry e = new EllipseGeometry(new Point(X, Y), Radius, Radius);
                GeometryGroup combined = new GeometryGroup();
                combined.Children.Add(e);
                combined.Children.Add(line);
                return combined;
            }
        }
    }
}
