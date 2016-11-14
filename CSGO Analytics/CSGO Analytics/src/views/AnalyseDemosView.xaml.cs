using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
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
using DP = DemoInfoModded;
using Newtonsoft.Json;
using CSGO_Analytics.src.json.jsonobjects;
using CSGO_Analytics.src.json.parser;

namespace CSGO_Analytics.src.views
{
    /// <summary>
    /// Interaction logic for AnalyseDemosView.xaml
    /// </summary>
    public partial class AnalyseDemosView : Page
    {
        /// <summary>
        /// Thread where all positional and situationgraph updates are handled(update player positions and links as well as nades etc)
        /// </summary>
        private Thread updateThread;

        private EncounterDetectionAlgorithm enDetect;

        private Gamestate gamestate;

        private MatchReplay matchreplay;

        //
        // MAP VARIABLES
        //
        private string mapname;
        private double scalefactor_map;
        private double map_width;
        private double map_height;
        private double map_x;
        private double map_y;


        private float tickrate;


        //
        // VISUALS
        //

        /// <summary>
        /// Panel where all players and links are being drawn
        /// </summary>
        private Canvas mapPanel = new Canvas();

        /// <summary>
        /// Component (holding mapPanel) used to drag around and zoom in
        /// </summary>
        private Viewbox map;

        /// <summary>
        /// All players drawn on the minimap
        /// </summary>
        private Dictionary<int, PlayerShape> playershapes = new Dictionary<int, PlayerShape>();

        /// <summary>
        /// All links between players that are currently drawn
        /// </summary>
        private List<LinkShape> links = new List<LinkShape>();

        /// <summary>
        /// Circles representing active nades(smoke=grey, fire=orange, henade=red, flash=white, decoy=outline only)
        /// </summary>
        private List<Ellipse> activeNades = new List<Ellipse>();




        public AnalyseDemosView()
        {
            try
            {
                var path = "match_0.dem";
                /*using (var demoparser = new DP.DemoParser(File.OpenRead(path)))
                 {
                     ParseTask ptask = new ParseTask
                     {
                         destpath = path,
                         srcpath = path,
                         usepretty = true,
                         showsteps = true,
                         specialevents = true,
                         highdetailplayer = true,
                         positioninterval = 8,
                         settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All, Formatting = Formatting.None }
                     };
                     GameStateGenerator.GenerateJSONFile(demoparser, ptask);
                      
            }*/


                using (var reader = new StreamReader(path.Replace(".dem", ".json")))
                {
                    gamestate = Newtonsoft.Json.JsonConvert.DeserializeObject<Gamestate>(reader.ReadToEnd(), new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All, Formatting = Formatting.None });
                    enDetect = new EncounterDetectionAlgorithm(gamestate);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }


            try
            {

                InitializeComponent();

                InitializeGUI();

                InitializeMap();

                InitializeEncounterDetection();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }

        }

        private void InitializeGUI()
        {
            this.tickrate = gamestate.meta.tickrate;
            this.mapname = gamestate.meta.mapname;
            time_slider.Minimum = 0;
            time_slider.Maximum = gamestate.match.rounds.Last().ticks.Last().tick_id;
        }


        /// <summary>
        /// MapPanel and minimap viewbox are initalized
        /// </summary>
        private void InitializeMap()
        {

            canvas.ClipToBounds = true;
            BitmapImage bi = new BitmapImage(new Uri(@"C:\Users\Dev\LRZ Sync+Share\Bacheloarbeit\CS GO Encounter Detection\csgo-stats-ed\CSGO Analytics\CSGO Analytics\src\views\maps\" + mapname + "_radar.jpg", UriKind.Relative));
            map_width = bi.Width; // Save original size to apply scaling
            map_height = bi.Height;
            mapPanel.Background = new ImageBrush(bi);

            scalefactor_map = canvas.Height / map_height;
            map = new Viewbox();
            map.StretchDirection = StretchDirection.Both;
            map.Stretch = Stretch.Fill;
            map.Child = mapPanel;

            mapPanel.Width = map.Width = bi.Width * scalefactor_map;
            mapPanel.Height = map.Height = bi.Height * scalefactor_map;

            Canvas.SetLeft(map, (canvas.Width - map.Width) / 2);
            Canvas.SetTop(map, (canvas.Height - map.Height) / 2);

            canvas.Children.Add(map);

        }







        public void InitializeEncounterDetection()
        {

            this.matchreplay = this.enDetect.run(); // Run the algorithm

            // Initalize all graphical player representations default/start
            foreach (var p in enDetect.getPlayers()) // TODO: old data loaded here -> players are drawn where they stood when freeze began
            {
                drawPlayer(p);
            }

        }

        private void Button_play(object sender, RoutedEventArgs e)
        {
            if (updateThread == null)
                updateThread = new Thread(new ThreadStart(playMatch));

            updateThread.Start();
        }

        private void Button_stop(object sender, RoutedEventArgs e)
        {
            if (updateThread != null) ;
            //TODO: pause and resume
        }

        private void playMatch()
        {

            if (matchreplay == null)
                return;
            int last_tickid = 0;
            foreach (var tuple in matchreplay.getReplayData())
            {
                Tick tick = tuple.Item1;
                CombatComponent comp = tuple.Item2;

                if (last_tickid == 0)
                    last_tickid = tick.tick_id;
                int dt = tick.tick_id - last_tickid;

                int passedTime = (int)(dt * tickrate);

                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate
                {
                    try
                    {
                        if (links.Count != 0)
                        {
                            foreach (var oldlink in links)
                                mapPanel.Children.Remove(oldlink);

                            links.Clear();
                        }

                        time_slider.Value = tick.tick_id;
                        tick_label.Content = "Tick: " + tick.tick_id;
                        var timesec = (int)(tick.tick_id * tickrate / 1000);
                        time_label.Content = "Time: " + ":" + timesec;
                        if (comp != null && comp.links.Count != 0)
                        {
                            foreach (var link in comp.links)
                            {
                                drawLink(link.getActor(), link.getReciever(), ComponentType.COMBATLINK);
                            }
                        }

                        foreach (var p in tick.getUpdatedPlayers())
                        {
                            updatePlayer(p); // Problems with threading as here the ui-thread will be called because shape properties are updated -> Call dispatcher :/
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.StackTrace);
                    }

                }));
                Thread.Sleep(passedTime);

                last_tickid = tick.tick_id;

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
            if (playershapes.TryGetValue(actor.player_id, out aps))
            {
                ls.X1 = aps.X;
                ls.Y1 = aps.Y;
            }
            else
            {
                Console.WriteLine("Could not map PlayerShape");
            }
            PlayerShape rps;
            if (playershapes.TryGetValue(reciever.player_id, out rps))
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


            links.Add(ls);
            mapPanel.Children.Add(ls);

        }

        private void updateLink(Player actor)
        {

        }

        private void drawPlayer(Player p)
        {
            var ps = new PlayerShape();
            ps.Yaw = p.facing.yaw;
            ps.X = MathLibrary.CSPositionToUIPosition(p.position).x;
            ps.Y = MathLibrary.CSPositionToUIPosition(p.position).y;
            ps.Radius = 4;
            Color color;
            if (p.getTeam() == Team.T)
                color = Color.FromArgb(255, 255, 0, 0);
            else
                color = Color.FromArgb(255, 0, 0, 255);

            ps.Fill = new SolidColorBrush(color);
            ps.Stroke = new SolidColorBrush(color);
            ps.StrokeThickness = 0.5;

            playershapes.Add(enDetect.getID(p.player_id), ps);
            mapPanel.Children.Add(ps);
        }


        private void updatePlayer(Player p)
        {

            //if(p.) // TODO: test if player is dead -> no updates and let him look different
            PlayerShape ps;
            if (playershapes.TryGetValue(enDetect.getID(p.player_id), out ps))
            {

                ps.X = MathLibrary.CSPositionToUIPosition(p.position).x;
                ps.Y = MathLibrary.CSPositionToUIPosition(p.position).y;
                ps.Yaw = p.facing.yaw;
            }
            else
            {
                Console.WriteLine("Could not map PlayerShape");
            }

        }

        private void captureScreenshot()
        {

            RenderTargetBitmap rtb = new RenderTargetBitmap(
                (int)canvas.RenderSize.Width + 95,
                (int)canvas.RenderSize.Height + 15,
                96d,
                96d,
                System.Windows.Media.PixelFormats.Default);

            rtb.Render(canvas);

            var crop = new CroppedBitmap(rtb, new Int32Rect(95, 15, (int)canvas.RenderSize.Width, (int)canvas.RenderSize.Height));

            BitmapEncoder pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(crop));

            using (var fs = System.IO.File.OpenWrite("logo.png"))
            {
                pngEncoder.Save(fs);
            }
        }

        private void createMatchAVI()
        {


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

            map.Width = map_width * scalefactor_map;
            map.Height = map_height * scalefactor_map;
            var mx = current.X;
            var my = current.Y;
            double x = (canvas.Width - map.Width) / 2.0;
            double y = (canvas.Height - map.Height) / 2.0;
            Canvas.SetLeft(map, x);
            Canvas.SetTop(map, y);

        }

        private Point start;
        private Point current;
        private Point focus;

        private bool isdragging;


        private void Canvas_OnLeftMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            isdragging = true;
            start = e.GetPosition(map);

        }

        private void Canvas_OnLefttMouseButtonUp(object sender, MouseButtonEventArgs e)
        {
            isdragging = false;
        }

        private void Canvas_OnMouseMove(object sender, MouseEventArgs e)
        {
            current = e.GetPosition(map);

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
            var x = Canvas.GetLeft(map);
            var y = Canvas.GetTop(map);
            Console.WriteLine(x);
            Console.WriteLine(y);
            var newx = x + dx * 0.3;
            var newy = y + dy * 0.3;
            Console.WriteLine(newx);
            Console.WriteLine(newy);
            Canvas.SetLeft(map, newx);
            Canvas.SetTop(map, newy);
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

        private void Page_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void TabControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S)
            {
                captureScreenshot();
            }
        }

        private void Canvas_KeyDown(object sender, KeyEventArgs e)
        {

        }



        //
        //
        // HELPING FUNCTIONS
        //
        //

    }
}
