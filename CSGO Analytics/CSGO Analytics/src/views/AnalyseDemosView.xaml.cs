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
        /*
         * TODO:
         * Mapping CS Punkte auf Zeichenpunkte
         * WIe wird gezeichnet? list der encounter? sind sie ausreichend organisiert?
         * Map bewegen implementieren? zoom drag map -> Spieler und links müssen mitbewegen
         */
        private EncounterDetectionAlgorithm enDetect;

        private double scalefactor_map = 0.6;
        private double map_width;
        private double map_height;
        private double map_x;
        private double map_y;

        private Canvas mapPanel = new Canvas();

        private Viewbox minimap;

        private Dictionary<int, PlayerShape> players = new Dictionary<int, PlayerShape>();
        private List<LinkShape> links = new List<LinkShape>();

        public AnalyseDemosView()
        {
            InitializeComponent();
            InitializeCanvas();
            //InitializeEncounterDetection();
        }

        private void InitializeCanvas()
        {
            canvas.ClipToBounds = true;
            BitmapImage bi = new BitmapImage(new Uri(@"C:\Users\Dev\LRZ Sync+Share\Bacheloarbeit\CS GO Encounter Detection\csgo-stats-ed\CSGO Analytics\CSGO Analytics\src\views\maps\de_dust2_map.jpg", UriKind.Relative));
            map_width = bi.Width; // Save original size to apply scaling
            map_height = bi.Height;
            mapPanel.Background = new ImageBrush(bi);

            minimap = new Viewbox();
            minimap.StretchDirection = StretchDirection.Both;
            minimap.Stretch = Stretch.Fill;
            minimap.Child = mapPanel;

            mapPanel.Width = minimap.Width = bi.Width * scalefactor_map;
            mapPanel.Height = minimap.Height = bi.Height * scalefactor_map;

            map_x = (canvas.Width - minimap.Width);
            map_y = (canvas.Height - minimap.Height);

            Canvas.SetLeft(minimap, map_x / 2);
            Canvas.SetTop(minimap, map_y / 2);

            canvas.Children.Add(minimap);

        }

        public void InitializeEncounterDetection()
        {
            this.enDetect = new EncounterDetectionAlgorithm(new json.jsonobjects.Gamestate()); // TODO fill

            //Initalize all graphical player representations TODO: positionsumrechnung
            foreach (var p in enDetect.getPlayers())
            {
                drawPlayer(p);
            }

            this.enDetect.run();

            foreach (var en in this.enDetect.getEncounters())
            {
                foreach (var c in en.cs)
                {
                    foreach (var p in c.players)
                    {
                        updatePlayer(p);
                    }
                }
            }
        }
        //
        //
        // ENCOUNTER DETECTION VISUALISATION: Draw players, links and line of sight as well as other events of the game
        //
        //

        private void drawLink(Player actor, Player reciever, ComponentType type)
        {
            LinkShape l = new LinkShape();

            PlayerShape aps;
            if (players.TryGetValue(actor.player_id, out aps))
            {
                l.X1 = aps.X;
                l.Y1 = aps.Y;
            }
            else
            {
                Console.WriteLine("Could not map PlayerShape");
            }
            PlayerShape rps;
            if (players.TryGetValue(reciever.player_id, out rps))
            {
                l.X2 = rps.X;
                l.Y2 = rps.Y;
            }
            else
            {
                Console.WriteLine("Could not map PlayerShape");
            }

            l.StrokeThickness = 2;
            l.Stroke = System.Windows.Media.Brushes.DarkRed;


            if (type == ComponentType.COMBATLINK)
                l.Stroke = System.Windows.Media.Brushes.DarkRed;
            else
                l.Stroke = System.Windows.Media.Brushes.DarkGreen;
            if (! links.Contains(l))
            {
                links.Add(l);
                mapPanel.Children.Add(l);
            } else
            {
                l = null;
            }

        }

        private void drawFOV()
        {

        }

        private void updatePlayer(Player p)
        {
            PlayerShape ps;
            if (players.TryGetValue(p.player_id, out ps))
            {
                ps.X = p.position.x;
                ps.Y = p.position.y;
                ps.Yaw = p.facing.yaw;
            }
            else
            {
                Console.WriteLine("Could not map PlayerShape");
            }

        }

        private void drawPlayer(Player p)
        {
            var ps = new PlayerShape();
            ps.Yaw = p.facing.yaw;
            ps.X = p.position.x;
            ps.Y = p.position.y;
            ps.Radius = 4;
            Color color;
            if (p.getTeam() == Team.T)
                color = Color.FromArgb(255, 255, 0, 0);
            else
                color = Color.FromArgb(255, 0, 0, 255);

            ps.Fill = new SolidColorBrush(color);
            ps.Stroke = new SolidColorBrush(color);
            ps.StrokeThickness = 0.5;

            //players.Add(enDetect.getID(p.player_id), ps);
            players.Add(p.player_id, ps);
            mapPanel.Children.Add(ps);
        }

        //
        //
        // EVENTS
        //
        //

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
            var newscale = scalefactor_map + dt * 0.001;
            if (newscale >= 0.5)
                scalefactor_map = newscale;
            else return;

            minimap.Width = map_width * scalefactor_map;
            minimap.Height = map_height * scalefactor_map;
            var mx = current.X;
            var my = current.Y;
            double x = (canvas.Width - minimap.Width) / 2.0;
            double y = (canvas.Height - minimap.Height) / 2.0;
            Canvas.SetLeft(minimap, x);
            Canvas.SetTop(minimap, y);

        }

        private Point start;
        private Point current;
        private Point focus;

        private bool isdragging;

        private void Canvas_OnLeftMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            isdragging = true;
            start = e.GetPosition(minimap);

            Player p = new Player()
            {
                position = new math.Vector((float)current.X, (float)current.Y, 20),
                facing = new Facing { yaw = 30, pitch = 20 },
                player_id = 1,
                team = "Terrorist"
            };
            updatePlayer(p);

            

        }

        private void Canvas_OnLefttMouseButtonUp(object sender, MouseButtonEventArgs e)
        {
            isdragging = false;
        }

        private void Canvas_OnMouseMove(object sender, MouseEventArgs e)
        {
            current = e.GetPosition(minimap);

            if (!isdragging)
                return;
            double dx = start.X - current.X;
            double dy = start.Y - current.Y;

            //moveMap(dx, dy);
        }
        int count = 0;
        private void moveMap(double dx, double dy)
        {
            
                Console.WriteLine(dx);
                Console.WriteLine(dy);
                var x = Canvas.GetLeft(minimap);
                var y = Canvas.GetTop(minimap);
                Console.WriteLine(x);
                Console.WriteLine(y);
                var newx = x + dx * 0.3;
                var newy = y + dy * 0.3;
                Console.WriteLine(newx);
                Console.WriteLine(newy);
                Canvas.SetLeft(minimap, newx);
                Canvas.SetTop(minimap, newy);
                count++;
            
        }


        public Canvas getCanvas()
        {
            return canvas;
        }

        private void Canvas_OnMouseLeave(object sender, MouseEventArgs e)
        {
            isdragging = false;
        }
    }
}
