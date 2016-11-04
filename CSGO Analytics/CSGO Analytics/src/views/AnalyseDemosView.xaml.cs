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
using CSGO_Analytics.src.encounterdetect;
using CSGO_Analytics.src.data.gameobjects;

namespace CSGO_Analytics.src.views
{
    /// <summary>
    /// Interaction logic for AnalyseDemosView.xaml
    /// </summary>
    public partial class AnalyseDemosView : Page
    {
        private EncounterDetectionAlgorithm enDetect;

        private double scalefactor_map = 0.5;
        private double map_width;
        private double map_height;

        private StackPanel mapPanel = new StackPanel();

        private Dictionary<Player, StackPanel> players = new Dictionary<Player, StackPanel>();

        public AnalyseDemosView()
        {
            InitializeComponent();
            //InitializeEncounterDetection();
            InitializeCanvas();
        }

        private void InitializeCanvas()
        {
            canvas.ClipToBounds = true;
            BitmapImage bi = new BitmapImage(new Uri(@"C:\Users\Dev\LRZ Sync+Share\Bacheloarbeit\CS GO Encounter Detection\csgo-stats-ed\CSGO Analytics\CSGO Analytics\src\views\maps\de_dust2_map.jpg", UriKind.Relative));

            map_width = bi.Width; // Save original size to apply scaling
            map_height = bi.Height;
            mapPanel.Background = new ImageBrush(bi);

            mapPanel.Width = bi.Width * scalefactor_map;
            mapPanel.Height = bi.Height * scalefactor_map;

            double maxX = canvas.Width - mapPanel.Width;
            double maxY = canvas.Height - mapPanel.Height;

            Canvas.SetLeft(mapPanel, maxX / 2);
            Canvas.SetTop(mapPanel, maxY / 2);
            canvas.Children.Add(mapPanel);

            StackPanel p = new StackPanel();
            p.Margin = new Thickness(0, 0, 0, 0);
            p.Background = new SolidColorBrush(Color.FromArgb(255,255,255,0));
            var e = new Ellipse();
            //p.Children.Add(e);

            e.Width = 8;
            e.Height = 8;
            e.Margin = new Thickness(0, 0, 0, 0);
            SolidColorBrush playerBrush = new SolidColorBrush();
            playerBrush.Color = Color.FromArgb(255, 255, 0, 0);
            e.Fill = playerBrush;

            var line = new Line();
            //p.Children.Add(line);

            line.Stroke = System.Windows.Media.Brushes.Green;
            line.X1 = 0;
            line.X2 = 50;
            line.Y1 = 0; //TODO: calc with yaw and pitch
            line.Y2 = 50;
            line.StrokeThickness = 1;

            mapPanel.Children.Add(p);
        }

        public void InitializeEncounterDetection()
        {
            this.enDetect = new EncounterDetectionAlgorithm(new json.jsonobjects.Gamestate()); // TODO fill

            foreach (var  p in enDetect.getPlayers())
            {
                var panel = new StackPanel();
                var e = new Ellipse();
                SolidColorBrush playerBrush = new SolidColorBrush();
                if(p.team == "CT")
                    playerBrush.Color = Color.FromArgb(0, 255, 0, 0);
                else
                    playerBrush.Color = Color.FromArgb(255, 0, 0, 0);

                e.Fill = playerBrush;
                e.Width = 3;
                e.Height = 3;
                panel.Children.Add(e);

                this.players.Add(p, panel);

            }

            this.enDetect.run();

            foreach (var en in this.enDetect.getEncounters())
            {
                foreach (var c in en.cs)
                {
                    foreach (var p in c.players)
                    {
                        updatePlayerGraphic(p);
                        drawLineOfSight(p);
                    }
                }
            }
        }


        //
        //
        // MAP FUNCTIONS: Drag and Zoom the map
        //
        //

        private void Canvas_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            int dt = e.Delta;
            zoomMap(dt);
        }

        private void zoomMap(int dt)
        {
            //TODO: zoom not working when to high and then return to 0.5 not possible
            var newscale = scalefactor_map + dt * 0.001;
            if (newscale >= 0.5 )
                scalefactor_map = newscale;
            else return;

            mapPanel.Width = map_width * scalefactor_map;
            mapPanel.Height = map_height * scalefactor_map;

            double maxX = canvas.Width - mapPanel.Width;
            double maxY = canvas.Height - mapPanel.Height;
            Canvas.SetLeft(mapPanel, maxX / 2);
            Canvas.SetTop(mapPanel, maxY / 2);
        }

        private Point start;
        private Point current;

        private bool isdragging;

        private void Canvas_OnLeftMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            isdragging = true;
            start = e.GetPosition(null);
        }

        private void Canvas_OnRightMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            isdragging = false;
            start = e.GetPosition(null);
        }

        private void Canvas_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!isdragging)
                return;

            current = e.GetPosition(null);

            double dx = start.X - current.X;
            double dy = start.Y - current.Y;

            moveMap(dx, dy);
        }

        private void moveMap(double dx, double dy)
        {
            double maxX = canvas.Width - mapPanel.Width;
            double maxY = canvas.Height - mapPanel.Height;
            Canvas.SetLeft(mapPanel, maxX / 2 + dx);
            Canvas.SetTop(mapPanel, maxY / 2 + dy);
        }


        //
        //
        // ENCOUNTER DETECTION VISUALISATION: Draw players, links and line of sight as well as other events of the game
        //
        //

        private void drawLineOfSight(Player p)
        {
            var px = p.position.x;
            var py = p.position.y;

            var yaw = p.facing.yaw;
            var pitch = p.facing.pitch;

            var line = new Line();
            line.Stroke = System.Windows.Media.Brushes.Red;
            line.X1 = px;
            line.X2 = py;
            line.Y1 = 1; //TODO: calc with yaw and pitch
            line.Y2 = 50;
            line.StrokeThickness = 2;
            mapPanel.Children.Add(line);
        }

        private void drawFOV()
        {

        }

        private void updatePlayerGraphic(Player p)
        {
            var x = p.position.x;
            var y = p.position.y;

            //Ellipse e;
            //if (!players.TryGetValue(p, out e))
                //return;

            //e.Margin = new Thickness(x, y, 0, 0);

            //mapPanel.Children.Add(e);
        }

 

        public Canvas getCanvas()
        {
            return canvas;
        }

        
    }
}
