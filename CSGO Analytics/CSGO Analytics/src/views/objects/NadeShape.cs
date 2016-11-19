using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows;
using CSGO_Analytics.src.data.gameobjects;
using CSGO_Analytics.src.encounterdetect;

namespace CSGO_Analytics.src.views
{
    class NadeShape : Shape
    {

        public double Radius { get; set; }

        public float Duration { get; set; }

        public double X
        {
            get { return (double)GetValue(XProperty); }
            set { SetValue(XProperty, value); }
        }

        private static FrameworkPropertyMetadata XMetadata =
                new FrameworkPropertyMetadata(
                    90.0,     // Default value
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    null,    // Property changed callback
                    null);   // Coerce value callback

        public static readonly DependencyProperty XProperty =
            DependencyProperty.Register("X", typeof(double), typeof(NadeShape), XMetadata);


        public double Y
        {
            get { return (double)GetValue(YProperty); }
            set { SetValue(YProperty, value); }
        }

        private static FrameworkPropertyMetadata YMetadata =
                new FrameworkPropertyMetadata(
                    0.0,     // Default value
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    null,    // Property changed callback
                    null);   // Coerce value callback

        public static readonly DependencyProperty YProperty =
            DependencyProperty.Register("Y", typeof(double), typeof(NadeShape), YMetadata);

      
        protected override Geometry DefiningGeometry
        {
            get
            {
                Geometry effectCircle = new EllipseGeometry(new Point(X, Y), Radius, Radius);


                GeometryGroup combined = new GeometryGroup();
                //combined.Children.Add(geometry);
                combined.Children.Add(effectCircle);
                return combined;
            }
        }
    }
}
