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
using CSGO_Analytics.src.math;

namespace CSGO_Analytics.src.views
{
    /// <summary>
    /// Interaction logic for AnalyseDemosView.xaml
    /// </summary>
    public partial class AnalyseDemosView : Page
    {

        private EncounterDetectionAlgorithm enDetect;

        private double scalefactor_map;
        private double map_width;
        private double map_height;
        private double map_x;
        private double map_y;

        /// <summary>
        /// Panel where all players and links are being drawn
        /// </summary>
        private Canvas mapPanel = new Canvas();

        /// <summary>
        /// Component (holding mapPanel) used to drag around and zoom in
        /// </summary>
        private Viewbox minimap;

        /// <summary>
        /// All players drawn on the minimap
        /// </summary>
        private Dictionary<int, PlayerShape> players = new Dictionary<int, PlayerShape>();

        /// <summary>
        /// All links between players that are currently drawn
        /// </summary>
        private List<LinkShape> links = new List<LinkShape>();

        private List<Ellipse> activeNades = new List<Ellipse>();

        public AnalyseDemosView()
        {
            InitializeComponent();

            InitializeCanvas();
            //InitializeEncounterDetection();
        }


        /// <summary>
        /// MapPanel and minimap viewbox are initalized
        /// </summary>
        private void InitializeCanvas()
        {
            canvas.ClipToBounds = true;
            BitmapImage bi = new BitmapImage(new Uri(@"C:\Users\Dev\LRZ Sync+Share\Bacheloarbeit\CS GO Encounter Detection\csgo-stats-ed\CSGO Analytics\CSGO Analytics\src\views\maps\de_dust2_map.jpg", UriKind.Relative));
            map_width = bi.Width; // Save original size to apply scaling
            map_height = bi.Height;
            mapPanel.Background = new ImageBrush(bi);

            scalefactor_map = canvas.Height / map_height;
            minimap = new Viewbox();
            minimap.StretchDirection = StretchDirection.Both;
            minimap.Stretch = Stretch.Fill;
            minimap.Child = mapPanel;

            mapPanel.Width = minimap.Width = bi.Width * scalefactor_map;
            mapPanel.Height = minimap.Height = bi.Height * scalefactor_map;

            Canvas.SetLeft(minimap, (canvas.Width - minimap.Width) / 2);
            Canvas.SetTop(minimap, (canvas.Height - minimap.Height) / 2);

            Player p = new Player()
            {
                position = new math.Vector((float)current.X, (float)current.Y, 20),
                facing = new Facing { yaw = 90, pitch = 20 },
                player_id = 1,
                team = "Terrorist"
            };

            drawPlayer(p);
            canvas.Children.Add(minimap);

        }

        public void InitializeEncounterDetection()
        {
            this.enDetect = new EncounterDetectionAlgorithm(new json.jsonobjects.Gamestate()); // Init Encounter Detection
                                                                                               
            this.enDetect.run(); // Run the algorithm

            // Initalize all graphical player representations default/start
            foreach (var p in enDetect.getPlayers())
            {
                drawPlayer(p);
            }

        }

        private void playMatch()
        {
            // Display each encounter with its links and players
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
            LinkShape ls = new LinkShape();

            PlayerShape aps;
            if (players.TryGetValue(actor.player_id, out aps))
            {
                ls.X1 = aps.X;
                ls.Y1 = aps.Y;
            }
            else
            {
                Console.WriteLine("Could not map PlayerShape");
            }
            PlayerShape rps;
            if (players.TryGetValue(reciever.player_id, out rps))
            {
                ls.X2 = rps.X;
                ls.Y2 = rps.Y;
            }
            else
            {
                Console.WriteLine("Could not map PlayerShape");
            }

            ls.StrokeThickness = 2;
            ls.Stroke = System.Windows.Media.Brushes.DarkRed;


            if (type == ComponentType.COMBATLINK)
                ls.Stroke = System.Windows.Media.Brushes.DarkRed;
            else
                ls.Stroke = System.Windows.Media.Brushes.DarkGreen;

            if(!links.Contains(ls))
            {
                links.Add(ls);
                mapPanel.Children.Add(ls);
            }
        }

        private void updateLink(Player actor)
        {

        }

        private void drawPlayer(Player p)
        {
            var ps = new PlayerShape();
            ps.Yaw = p.facing.yaw;
            ps.X = MathUtils.CSPositionToUIPosition(p.position).x;
            ps.Y = MathUtils.CSPositionToUIPosition(p.position).y;
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


        private void updatePlayer(Player p)
        {
            //if(p.) // TODO: test if player is dead -> no updates and let him look different
            PlayerShape ps;
            if (players.TryGetValue(p.player_id, out ps))
            {
                ps.X = MathUtils.CSPositionToUIPosition(p.position).x;
                ps.Y = MathUtils.CSPositionToUIPosition(p.position).y;
                ps.Yaw = p.facing.yaw;
            }
            else
            {
                Console.WriteLine("Could not map PlayerShape");
            }

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
                facing = new Facing { yaw = 45, pitch = 20 },

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

        private void Canvas_OnMouseLeave(object sender, MouseEventArgs e)
        {
            isdragging = false;
        }

        public Canvas getCanvas()
        {
            return canvas;
        }


        //
        //
        // HELPING FUNCTIONS
        //
        //

    }
}
