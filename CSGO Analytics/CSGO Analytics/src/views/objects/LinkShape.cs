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
    class LinkShape : Shape
    {
        private Direction linkdirection;

        public double Length { get; set; }

        public double X1
        {
            get { return (double)GetValue(X1Property); }
            set { SetValue(X1Property, value); }
        }

        private static FrameworkPropertyMetadata X1Metadata =
                new FrameworkPropertyMetadata(
                    90.0,     // Default value
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    null,    // Property changed callback
                    null);   // Coerce value callback

        public static readonly DependencyProperty X1Property =
            DependencyProperty.Register("X1", typeof(double), typeof(LinkShape), X1Metadata);


        public double X2
        {
            get { return (double)GetValue(X2Property); }
            set { SetValue(X2Property, value); }
        }

        private static FrameworkPropertyMetadata X2Metadata =
                new FrameworkPropertyMetadata(
                    0.0,     // Default value
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    null,    // Property changed callback
                    null);   // Coerce value callback

        public static readonly DependencyProperty X2Property =
            DependencyProperty.Register("X2", typeof(double), typeof(LinkShape), X2Metadata);

        public double Y1
        {
            get { return (double)GetValue(Y1Property); }
            set { SetValue(Y1Property, value); }
        }

        private static FrameworkPropertyMetadata Y1Metadata =
                new FrameworkPropertyMetadata(
                    0.0,     // Default value
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    null,    // Property changed callback
                    null);   // Coerce value callback

        public static readonly DependencyProperty Y1Property =
            DependencyProperty.Register("Y1", typeof(double), typeof(LinkShape), Y1Metadata);

        public double Y2
        {
            get { return (double)GetValue(Y2Property); }
            set { SetValue(Y2Property, value); }
        }

        private static FrameworkPropertyMetadata Y2Metadata =
                new FrameworkPropertyMetadata(
                    0.0,     // Default value
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    null,    // Property changed callback
                    null);   // Coerce value callback

        public static readonly DependencyProperty Y2Property =
            DependencyProperty.Register("Y2", typeof(double), typeof(LinkShape), Y2Metadata);



        protected override Geometry DefiningGeometry
        {
            get
            {
                Point start = new Point(X1, Y1);
                Point end = new Point(X2, Y2);
                Geometry line = new LineGeometry(start, end);

                /*StreamGeometry geometry = new StreamGeometry();
                geometry.FillRule = FillRule.EvenOdd;

                //Arrow tops for links TODO
                 using (StreamGeometryContext ctx = geometry.Open())
                {
                    // Begin the triangle at the point specified.
                    if (linkdirection == Direction.DEFAULT)
                    {
                        ctx.BeginFigure(new Point(10, 100), true, true);

                    } else
                    {

                    }
                    ctx.BeginFigure(new Point(10, 100), true, true);
                    ctx.LineTo(new Point(100, 100), true, false);
                    ctx.LineTo(new Point(100, 50), true , false );
                }*/

                // Freeze the geometry for performance benefits.
                //geometry.Freeze();


                GeometryGroup combined = new GeometryGroup();
                //combined.Children.Add(geometry);
                combined.Children.Add(line);
                return combined;
            }
        }
    }
}
