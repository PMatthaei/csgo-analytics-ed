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
using CSGO_Analytics.src.data.gameevents;
using CSGO_Analytics.src.math;
using DP = DemoInfoModded;
using Newtonsoft.Json;
using CSGO_Analytics.src.json.jsonobjects;
using CSGO_Analytics.src.json.parser;
using CSGO_Analytics.src.utils;

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
        // VARIABLES FOR GAMEVISUALS
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
        // VISUALS OF THE GAME-REPRESENTATION
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
        private List<NadeShape> activeNades = new List<NadeShape>();




        public AnalyseDemosView()
        {
            try
            {
                InitializeComponent();

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
                    this.gamestate = Newtonsoft.Json.JsonConvert.DeserializeObject<Gamestate>(reader.ReadToEnd(), new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All, Formatting = Formatting.None });
                    this.enDetect = new EncounterDetectionAlgorithm(gamestate);
                }

                //LoadMapData();

                InitializeGUIData();

                InitializeMapGraphic();

                MathLibrary.initalizeConstants(LoadMapData());

                InitializeEncounterDetection();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }



        }

        private MapMetaData LoadMapData()
        {
            string path = @"C:\Users\Dev\LRZ Sync+Share\Bacheloarbeit\CS GO Encounter Detection\csgo-stats-ed\CSGO Analytics\CSGO Analytics\src\views\maps\" + mapname + ".txt";
            return new MapMetaDataPropertyReader(path).metadata;
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

                //Run UI changes in a Non-UI-Blocking thread. Problems with threading as here the ui-thread will be called because shape properties are updated -> Call dispatcher :/
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate
                {

                    if (links.Count != 0)
                    {
                        links.ForEach(l => mapPanel.Children.Remove(l)); //TODO: das muss weg. stattdessen sollen links sterben wenn sie nicht mehr gebraucht werden. wann ist das?
                        links.Clear();
                    }

                    // Update UI: timers, labels etc
                    updateUI(tick);

                    // Update map with all active components, player etc 
                    foreach (var p in tick.getUpdatedPlayers())
                    {
                        updatePlayer(p);
                    }


                    if (comp != null && comp.links.Count != 0)
                    {
                        foreach (var link in comp.links)
                        {
                            if (hasActiveLinkShape(link)) // Old link -> update else draw new
                                updateLink(link);
                            else
                                drawLink(link.getActor(), link.getReciever(), link.getLinkType());
                        }
                    }


                    // Draw all event relevant graphics(nades, bombs etc)
                    if (tick.getNadeEvents().Count != 0)
                    {
                        foreach (var n in tick.getNadeEvents())
                        {
                            drawNade(n);
                        }
                    }
                    if (tick.getNadeEndEvents().Count != 0)
                    {
                        foreach (var n in tick.getNadeEndEvents())
                        {
                            updateNades(n);
                        }
                    }
                }));
                Thread.Sleep(passedTime);

                last_tickid = tick.tick_id;

            }
        }

        private void updateUI(Tick tick)
        {
            time_slider.Value = tick.tick_id;
            tick_label.Content = "Tick: " + tick.tick_id;

            double ticks = (double)(tick.tick_id * tickrate);
            TimeSpan time = TimeSpan.FromMilliseconds(ticks);
            DateTime startdate = new DateTime(1970, 1, 1) + time;
            time_label.Content = startdate.Minute + ":" + startdate.Second + ":" + startdate.Millisecond;
        }

        private bool hasActiveLinkShape(Link link)
        {
            foreach (var l in links)
            {
                if (l.actor.Equals(link.getActor()) || l.actor.Equals(link.getReciever()) && l.reciever.Equals(link.getActor()) || l.reciever.Equals(link.getReciever()))
                {
                    return true;
                }
            }
            return false;
        }



        //
        //
        // ENCOUNTER DETECTION VISUALISATION: Draw players, links and line of sight as well as other events of the game
        //
        //

        private void drawNade(NadeEvents n)
        {
            math.Vector nadepos = MathLibrary.CSPositionToUIPosition(n.position);
            NadeShape ns = new NadeShape();
            ns.X = nadepos.x;
            ns.Y = nadepos.y;
            ns.Radius = 20;
            ns.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

            activeNades.Add(ns);
            mapPanel.Children.Add(ns);
        }

        private void updateNades(NadeEvents n)
        {
            math.Vector nadepos = MathLibrary.CSPositionToUIPosition(n.position);

            foreach (var ns in activeNades)
            {
                if (ns.X == nadepos.x && ns.Y == nadepos.y)
                {
                    activeNades.Remove(ns);
                    mapPanel.Children.Remove(ns);
                    break;
                }
            }

        }

        private void drawLink(Player actor, Player reciever, LinkType type)
        {
            LinkShape ls = new LinkShape(actor, reciever);

            PlayerShape aps = playershapes[enDetect.getTableID(actor)];
            ls.X1 = aps.X;
            ls.Y1 = aps.Y;

            PlayerShape rps = playershapes[enDetect.getTableID(reciever)];
            ls.X2 = rps.X;
            ls.Y2 = rps.Y;

            ls.StrokeThickness = 2;

            if (type == LinkType.COMBATLINK)
                ls.Stroke = System.Windows.Media.Brushes.DarkRed;
            else if (type == LinkType.SUPPORTLINK)
                ls.Stroke = System.Windows.Media.Brushes.DarkGreen;

            links.Add(ls);
            mapPanel.Children.Add(ls);

        }

        private void updateLink(Link link)
        {
            Player actor = link.getActor();
            Player reciever = link.getReciever();
            var psr = playershapes[enDetect.getTableID(reciever)];
            var psa = playershapes[enDetect.getTableID(actor)];

            foreach (var ls in links)
            {
                if (ls.actor.Equals(actor))
                {
                    ls.X1 = psa.X;
                    ls.Y1 = psa.Y;
                }
                else if (ls.actor.Equals(reciever))
                {
                    ls.X1 = psr.X;
                    ls.Y1 = psr.Y;
                }

                if (ls.reciever.Equals(actor))
                {
                    ls.X2 = psa.X;
                    ls.Y2 = psa.Y;
                }
                else if (ls.reciever.Equals(reciever))
                {
                    ls.X2 = psr.X;
                    ls.Y2 = psr.Y;
                }
            }
        }

        private void drawPlayer(Player p)
        {
            var ps = new PlayerShape();
            ps.Yaw = MathLibrary.toRadian(-p.facing.yaw);
            var vector = MathLibrary.CSPositionToUIPosition(p.position);
            ps.X = vector.x;
            ps.Y = vector.y;
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

            playershapes[enDetect.getTableID(p)] = ps;
            mapPanel.Children.Add(ps);
        }

        private Color deadcolor = Color.FromArgb(255, 0, 0, 0);

        private void updatePlayer(Player p)
        {

            PlayerShape ps = playershapes[enDetect.getTableID(p)];
            if (!ps.Active)
            {
                ps.Fill = new SolidColorBrush(deadcolor);
                ps.Stroke = new SolidColorBrush(deadcolor);
                return;
            }
            var vector = MathLibrary.CSPositionToUIPosition(p.position);
            ps.X = vector.x;
            ps.Y = vector.y;
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


        private void moveMapBy(double dx, double dy)
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
