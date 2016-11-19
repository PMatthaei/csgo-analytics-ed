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
        private int mapcenter_x;
        private int mapcenter_y;
        private double scale;
        private int rotate;
        private double zoom;

        private float tickrate;


        //
        // VISUALS
        //

        /// <summary>
        /// Panel where all players and links are being drawn on
        /// </summary>
        private Canvas mapPanel = new Canvas();

        /// <summary>
        /// Component (holding mapPanel) used to drag around and zoom in
        /// </summary>
        private Viewbox map;

        /// <summary>
        /// All players drawn on the minimap
        /// </summary>
        private PlayerShape[] playershapes;

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
                         highdetailplay
                         er = true,
                         positioninterval = 8,
                         settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All, Formatting = Formatting.None }
                     };
                     GameStateGenerator.GenerateJSONFile(demoparser, ptask);
                      
            } */


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

                InitializeGUIData();

                InitializeMapGraphic();

                MathLibrary.initalizeConstants();

                InitializeEncounterDetection();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }

        }

        private void InitializeGUIData()
        {
            this.tickrate = gamestate.meta.tickrate;
            this.mapname = gamestate.meta.mapname;
            playershapes = new PlayerShape[gamestate.meta.players.Count];
            time_slider.Minimum = 0;
            time_slider.Maximum = gamestate.match.rounds.Last().ticks.Last().tick_id;

        }


        /// <summary>
        /// MapPanel and minimap viewbox are initalized
        /// </summary>
        private void InitializeMapGraphic()
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


            Console.WriteLine(mapPanel.Width);
            Console.WriteLine(mapPanel.Height);
            Canvas.SetLeft(map, (canvas.Width - map.Width) / 2);
            Canvas.SetTop(map, (canvas.Height - map.Height) / 2);

            canvas.Children.Add(map);

        }







        public void InitializeEncounterDetection()
        {

            this.matchreplay = this.enDetect.run(); // Run the algorithm

            // Initalize all graphical player representations default/start
            foreach (var p in gamestate.meta.players) // TODO: old data loaded here -> players are drawn where they stood when freeze began
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

                //Run UI changes in a Non-UI-Blocking thread 
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate
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

                    // Draw all event relevant graphics(nades, bombs etc)
                    foreach (var p in tick.tickevents.Where(t => t.gameevent == "smoke_exploded" || t.gameevent == "flash_exploded" || t.gameevent == "fire_exploded"))
                    {

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

            PlayerShape aps = playershapes[enDetect.getID(actor.player_id)];
            ls.X1 = aps.X;
            ls.Y1 = aps.Y;


            PlayerShape rps = playershapes[enDetect.getID(reciever.player_id)];
            ls.X2 = rps.X;
            ls.Y2 = rps.Y;

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
            ps.Yaw = MathLibrary.toRadian(-p.facing.yaw);
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
            ps.Active = true;
            playershapes[enDetect.getID(p.player_id)] = ps;
            mapPanel.Children.Add(ps);
        }

        Color deadcolor = Color.FromArgb(255, 0, 0, 0);

        private void updatePlayer(Player p)
        {

            PlayerShape ps = playershapes[enDetect.getID(p.player_id)];
            if (!ps.Active)
            {
                ps.Fill = new SolidColorBrush(deadcolor);
                ps.Stroke = new SolidColorBrush(deadcolor);
                return;
            }

            ps.X = MathLibrary.CSPositionToUIPosition(p.position).x;
            ps.Y = MathLibrary.CSPositionToUIPosition(p.position).y;
            ps.Yaw = MathLibrary.toRadian(-p.facing.yaw);

        }

        private bool screenshotcooldown = false;
        private void captureScreenshot()
        {
            if (screenshotcooldown)
            {
                return;
            }
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

            using (var fs = File.OpenWrite(mapname + "_screenshot_" + GetTimestamp() + ".png"))
            {
                pngEncoder.Save(fs);
            }
            eventlabel.Content = "Captured screenshot: " + mapname + "_screenshot_" + GetTimestamp() + ".png";
            screenshotcooldown = true;

            Task.Factory.StartNew(() => Thread.Sleep(2 * 1000)) // Wait 2 sec for nex screenshot
            .ContinueWith((t) =>
            {
                eventlabel.Content = "";
                screenshotcooldown = false;

            }, TaskScheduler.FromCurrentSynchronizationContext());
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


        private void moveMap(double dx, double dy)
        {
            var x = Canvas.GetLeft(map);
            var y = Canvas.GetTop(map);

            var newx = x + dx * 0.3;
            var newy = y + dy * 0.3;

            Canvas.SetLeft(map, newx);
            Canvas.SetTop(map, newy);
        }

        private void Canvas_OnMouseLeave(object sender, MouseEventArgs e)
        {
            isdragging = false;
        }

        public Canvas getCanvas()
        {
            return canvas;
        }

        private void TabControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S)
            {
                captureScreenshot();
            }
        }




        //
        //
        // HELPING FUNCTIONS
        //
        //

        public static string GetTimestamp()
        {
            long ticks = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
            ticks /= 10000000; //Convert windows ticks to seconds
            return ticks.ToString();

        }
    }
}
