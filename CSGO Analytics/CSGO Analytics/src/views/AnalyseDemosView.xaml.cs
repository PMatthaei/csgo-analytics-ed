using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CSGO_Analytics.src.views
{
    /// <summary>
    /// Interaction logic for AnalyseDemosView.xaml
    /// </summary>
    public partial class AnalyseDemosView : Page
    {
        public AnalyseDemosView()
        {
            InitializeComponent();
            test();
        }

        public void test()
        {
            Ellipse e = new Ellipse { Fill = new SolidColorBrush(Colors.Red),Width = 200, Height = 200, Margin = new Thickness { Left=200, Top = 200 } };

            canvas.Children.Add(e);
        }
    }
}
