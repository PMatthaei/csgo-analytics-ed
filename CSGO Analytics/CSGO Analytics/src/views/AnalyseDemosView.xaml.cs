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
using EDM = CSGO_Analytics.src.math;
using DP = DemoInfoModded;
using Newtonsoft.Json;
using CSGO_Analytics.src.json.jsonobjects;
using CSGO_Analytics.src.json.parser;
using System.ComponentModel;

namespace CSGO_Analytics.src.views
{
    /// <summary>
    /// Interaction logic for AnalyseDemosView.xaml
    /// </summary>
    public partial class AnalyseDemosView : Page
    {
        /// <summary>
        /// Backgroundworker to handle the replay. Especially preventing UI-Thread Blocking!
        /// </summary>
        private BackgroundWorker _replaybw = new BackgroundWorker();

        private EncounterDetectionAlgorithm EDAlgorithm;

        /// <summary>
        /// The gamestate to apply the encounter detection on
        /// </summary>
        private Gamestate gamestate;

        /// <summary>
        /// The replay returned by the algorithm
        /// </summary>
        private MatchReplay matchreplay;

        /// <summary>
        /// Metadata about the map played in the match
        /// </summary>
        private MapMetaData mapmeta;


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


        //
        // UI Variables
        //
        private float tickrate;


        //
        // VISUALS OF THE GAME-REPRESENTATION
        //

        /// <summary>
        /// Panel where all players and links are being drawn on with the corresponding map as background
        /// </summary>
        private Canvas mapPanel = new Canvas();

        /// <summary>
        /// Component (holding mapPanel) used to drag around and zoom in
        /// </summary>
        private Viewbox map = new Viewbox();

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
            InitializeComponent();

            InitializeAnalysetools();
        }

        private void InitializeAnalysetools()
        {
            BackgroundWorker _initbw = new BackgroundWorker();

            _initbw.DoWork += (sender, args) =>
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
                        this.gamestate = Newtonsoft.Json.JsonConvert.DeserializeObject<Gamestate>(reader.ReadToEnd(), new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All, Formatting = Formatting.None });
                        this.EDAlgorithm = new EncounterDetectionAlgorithm(gamestate);
                    }

                    InitializeEncounterDetection();

                    InitializeGUIData();

                    LoadMapData();

                    InitalizeMapConstants();

                    InitializeMapGraphic();
                };

            _initbw.RunWorkerCompleted += (sender, args) =>
            {
                if (args.Error != null)
                    MessageBox.Show(args.Error.ToString());
            };

            _initbw.RunWorkerAsync();
        }


        private void LoadMapData()
        {

            this.mapname = gamestate.meta.mapname;
            string path = @"E:\LRZ Sync+Share\Bacheloarbeit\CS GO Encounter Detection\csgo-stats-ed\CSGO Analytics\CSGO Analytics\src\views\mapviews\" + mapname + ".txt";
            //string path = @"C:\Users\Patrick\LRZ Sync+Share\Bacheloarbeit\CS GO Encounter Detection\csgo-stats-ed\CSGO Analytics\CSGO Analytics\src\views\mapviews\" + mapname + ".txt";
            this.mapmeta = MapMetaDataPropertyReader.readProperties(path);
            Console.WriteLine("Loaded Mapdata");
        }

        private void InitializeGUIData()
        {

            this.tickrate = gamestate.meta.tickrate;
            playershapes = new PlayerShape[gamestate.meta.players.Count];
            //Jump out of Background to update UI
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                time_slider.Minimum = 0;
                time_slider.Maximum = gamestate.match.rounds.Last().ticks.Last().tick_id;
            }));
            Console.WriteLine("Initialized GUI");

        }


        /// <summary>
        /// MapPanel and minimap viewbox are initalized
        /// </summary>
        private void InitializeMapGraphic()
        {
            // Jump out of Background to update UI
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                BitmapImage bi = new BitmapImage(new Uri(@"E:\LRZ Sync+Share\Bacheloarbeit\CS GO Encounter Detection\csgo-stats-ed\CSGO Analytics\CSGO Analytics\src\views\mapviews\" + mapname + "_radar.jpg", UriKind.Relative));
                map_width = bi.Width; // Save original size to apply scaling
                map_height = bi.Height;
                mapPanel.Background = new ImageBrush(bi);

                scalefactor_map = canvas.Height / map_height;
                map.StretchDirection = StretchDirection.Both;
                map.Stretch = Stretch.Fill;
                map.Child = mapPanel;

                mapPanel.Width = map.Width = bi.Width * scalefactor_map;
                mapPanel.Height = map.Height = bi.Height * scalefactor_map;

                Canvas.SetLeft(map, (canvas.Width - map.Width) / 2);
                Canvas.SetTop(map, (canvas.Height - map.Height) / 2);
                canvas.Children.Add(map);

                // Initalize all graphical player representations default/start
                foreach (var p in gamestate.meta.players) // TODO: old data loaded here -> players are drawn where they stood when freeze began
                    drawPlayer(p);

                Console.WriteLine("Initialized Map Graphics");
            }));
        }


        public void InitializeEncounterDetection()
        {
            this.matchreplay = this.EDAlgorithm.run(); // Run the algorithm

            Console.WriteLine("Initialized ED");
        }


        private void playMatch()
        {
            Console.WriteLine("Play Match");
            Console.WriteLine("Matchreplay found: " + matchreplay != null);

            if (matchreplay == null)
                return;

            _replaybw.DoWork += (sender, args) =>
            {
                int last_tickid = 0;

                foreach (var tuple in matchreplay.getReplayData())
                {
                    if (_replaybw.IsBusy) // Give _busy a chance to reset backgroundworker
                        _busy.WaitOne();

                    Tick tick = tuple.Key;
                    CombatComponent comp = tuple.Value;

                    if (last_tickid == 0)
                        last_tickid = tick.tick_id;
                    int dt = tick.tick_id - last_tickid;

                    int passedTime = (int)(dt * tickrate);// + 2000;

                    //Jump out of background to update UI
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        renderTick(tick, comp);
                    }));

                    Thread.Sleep(passedTime);

                    last_tickid = tick.tick_id;
                }
            };

            _replaybw.RunWorkerAsync();

            _replaybw.RunWorkerCompleted += (sender, args) =>
            {
                if (args.Error != null)
                    MessageBox.Show(args.Error.ToString());
            };
        }

        private void renderTick(Tick tick, CombatComponent comp)
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
        }




        //
        //
        // ADDITIONAL VISUALS (RENDER POSITIONS ETC)
        //
        //
        #region Rendering additional visuals
        private BackgroundWorker _renderbw = new BackgroundWorker();

        public void renderMapLevels()
        {
            Console.WriteLine("Render Map Levels");
            _renderbw.DoWork += (sender, args) =>
            {

                for (int i = 0; i < this.EDAlgorithm.map.maplevels.Count(); i++)
                {
                    Console.WriteLine("Level: " + i);
                    Color color = Color.FromArgb(255, 0, 0, 0);
                    switch (i)
                    {
                        case 0:
                            color = Color.FromArgb(255, 255, 0, 0); break; //rot
                        case 1:
                            color = Color.FromArgb(255, 0, 255, 0); break; //grün
                        case 2:
                            color = Color.FromArgb(255, 0, 0, 255); break; //blau
                        case 3:
                            color = Color.FromArgb(255, 255, 255, 0); break; //gelb
                        case 4:
                            color = Color.FromArgb(255, 0, 255, 255); break; //türkis
                        case 5:
                            color = Color.FromArgb(255, 255, 0, 255); break; //lilarosa
                        case 6:
                            color = Color.FromArgb(255, 120, 0, 0); break; //lilarosa
                        case 7:
                            color = Color.FromArgb(255, 0, 120, 0); break; //lilarosa
                        case 8:
                            color = Color.FromArgb(255, 0, 120, 120); break; //lilarosa
                        case 9:
                            color = Color.FromArgb(255, 120, 0, 120); break; //lilarosa
                        case 10:
                            color = Color.FromArgb(255, 120, 120, 0); break; //lilarosa
                    }


                    foreach (var r in this.EDAlgorithm.map.maplevels[i].level_cells)
                    {
                        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                        {
                            drawRect(r, color);
                        }));
                    }

                    Thread.Sleep(2000);
                }
            };
            _renderbw.RunWorkerAsync();

            _renderbw.RunWorkerCompleted += (sender, args) =>
            {
                if (args.Error != null)
                    MessageBox.Show(args.Error.ToString());
            };
        }

        public void renderHurtPositions(Tick tick)
        {
            foreach (var e in tick.tickevents)
            {
                if (e.gameevent == "player_hurt" || e.gameevent == "player_death")
                {
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        drawPos(e.getPositions()[0], Color.FromArgb(255, 255, 0, 0));
                        drawPos(e.getPositions()[1], Color.FromArgb(255, 0, 255, 0));
                    }));
                }
            }
        }
        #endregion




        //
        //
        // ENCOUNTER DETECTION VISUALISATION: Draw players, links and line of sight as well as other events of the game
        //
        //
        #region Drawing and Updating graphics and UI

        private void updateUI(Tick tick)
        {
            time_slider.Value = tick.tick_id;
            tick_label.Content = "Tick: " + tick.tick_id;

            double ticks = (double)(tick.tick_id * tickrate);
            TimeSpan time = TimeSpan.FromMilliseconds(ticks);
            DateTime startdate = new DateTime(1970, 1, 1) + time;
            time_label.Content = startdate.Minute + ":" + startdate.Second + ":" + startdate.Millisecond;
        }

        private void drawNade(NadeEvents n)
        {
            math.EDVector3D nadepos = CSPositionToUIPosition(n.position);
            NadeShape ns = new NadeShape();
            ns.X = nadepos.x;
            ns.Y = nadepos.y;
            ns.Radius = 20;
            Color color = Color.FromArgb(0, 0, 0, 0);
            switch (n.gameevent)
            {
                case "hegrenade_exploded":
                    color = Color.FromArgb(255, 255, 85, 50);
                    break;
                case "flash_exploded":
                    color = Color.FromArgb(255, 244, 255, 184);
                    break;
                case "smoke_exploded":
                    color = Color.FromArgb(255, 220, 220, 220);
                    break;
                case "firenade_exploded":
                    color = Color.FromArgb(255, 255, 132, 0);
                    break;
            }
            ns.Fill = new SolidColorBrush(color);

            activeNades.Add(ns);
            mapPanel.Children.Add(ns);
        }

        private void updateNades(NadeEvents n)
        {
            math.EDVector3D nadepos = CSPositionToUIPosition(n.position);

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

            PlayerShape aps = playershapes[EDAlgorithm.getTableID(actor)];
            ls.X1 = aps.X;
            ls.Y1 = aps.Y;

            PlayerShape rps = playershapes[EDAlgorithm.getTableID(reciever)];
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
            var psr = playershapes[EDAlgorithm.getTableID(reciever)];
            var psa = playershapes[EDAlgorithm.getTableID(actor)];

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

        private void drawPos(math.EDVector3D position, Color color)
        {
            var ps = new System.Windows.Shapes.Ellipse();
            var vector = CSPositionToUIPosition(position);
            ps.Margin = new Thickness(vector.x, vector.y, 0, 0);
            ps.Width = 4;
            ps.Height = 4;

            ps.Fill = new SolidColorBrush(color);
            ps.Stroke = new SolidColorBrush(color);
            ps.StrokeThickness = 0.5;

            mapPanel.Children.Add(ps);
        }

        private void drawRect(math.EDRect rect, Color color)
        {
            var ps = new System.Windows.Shapes.Rectangle();
            var vector = CSPositionToUIPosition(new math.EDVector3D((float)rect.X, (float)rect.Y, 0));
            ps.Margin = new Thickness(vector.x, vector.y, 0, 0);
            ps.Width = rect.Width * (Math.Min(mappanel_width, mapdata_width) / Math.Max(mappanel_width, mapdata_width));
            ps.Height = rect.Height * (Math.Min(mappanel_height, mapdata_height) / Math.Max(mappanel_height, mapdata_height));
           
            ps.Fill = new SolidColorBrush(color);
            ps.Stroke = new SolidColorBrush(Color.FromRgb(0,0,0));
            ps.StrokeThickness = 0.5;

            mapPanel.Children.Add(ps);
        }

        private void drawPlayer(Player p)
        {
            var ps = new PlayerShape();
            ps.Yaw = EDM.EDMathLibrary.toRadian(-p.facing.yaw);
            var vector = CSPositionToUIPosition(p.position);
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

            playershapes[EDAlgorithm.getTableID(p)] = ps;
            mapPanel.Children.Add(ps);
        }

        private Color deadcolor = Color.FromArgb(255, 0, 0, 0);

        private void updatePlayer(Player p)
        {
            PlayerShape ps = playershapes[EDAlgorithm.getTableID(p)];
            if (p.HP <= 0)
                ps.Active = false;
            if (p.HP > 0)
                ps.Active = true;

            if (!ps.Active)
            {
                ps.Fill = new SolidColorBrush(deadcolor);
                ps.Stroke = new SolidColorBrush(deadcolor);
                return;
            }
            else if (ps.Active)
            {
                Color color;
                if (p.getTeam() == Team.T)
                {
                    color = Color.FromArgb(255, 255, 0, 0);
                }
                else
                {
                    color = Color.FromArgb(255, 0, 0, 255);
                }

                ps.Fill = new SolidColorBrush(color);
                ps.Stroke = new SolidColorBrush(color);
            }

            if (p.isSpotted)
            {
                if (p.getTeam() == Team.T)
                    ps.Fill = new SolidColorBrush(Color.FromArgb(255, 225, 160, 160));
                else
                    ps.Fill = new SolidColorBrush(Color.FromArgb(255, 160, 160, 225));
            }
            else if (!p.isSpotted)
            {

                if (p.getTeam() == Team.T)
                    ps.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                else
                    ps.Fill = new SolidColorBrush(Color.FromArgb(255, 0, 0, 255));
            }

            var vector = CSPositionToUIPosition(p.position);
            ps.X = vector.x;
            ps.Y = vector.y;
            ps.Yaw = EDM.EDMathLibrary.toRadian(-p.facing.yaw);

        }
        #endregion




        //
        // UI Functionality - Screenshots, Render Match as AVI etc
        //
        #region Functionality
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
        #endregion




        //
        //
        // EVENTS
        //
        //
        #region Events
        //
        //
        // MAP FUNCTIONS: Drag and Zoom the map
        //
        //

        /// <summary>
        /// used to pause and resume the backgroundworker threads
        /// </summary>
        private ManualResetEvent _busy = new ManualResetEvent(true);
        private bool paused = false;

        private void Button_play(object sender, RoutedEventArgs e)
        {
            renderMapLevels();
            /*if (paused)
                _busy.Set();
            else
                playMatch();*/
        }

        private void Button_stop(object sender, RoutedEventArgs e)
        {
            _busy.Reset();
            paused = true;
        }

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
        #endregion




        //
        //
        // HELPING FUNCTIONS
        //
        //
        #region Helpers
        public static string GetTimestamp()
        {
            long ticks = DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks;
            ticks /= 10000000; //Convert windows ticks to seconds
            return ticks.ToString();

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

        /// <summary>
        /// Every CSGO Map has its center from where positions are calculated. We need this to produce our own coords. This is read by PropertieReader
        /// </summary>
        private static EDM.EDVector3D map_origin;

        //Size of Map in CSGO
        private static double mapdata_width;
        private static double mapdata_height;
        // Size of Image (Bitmap)
        private static double mappanel_width;
        private static double mappanel_height;

        public void InitalizeMapConstants() //TODO: initalize this with Data read from files about the current maps
        {

            map_origin = new EDM.EDVector3D((float)mapmeta.mapcenter_x, (float)mapmeta.mapcenter_y, 0);
            mapdata_width = 4500;
            mapdata_height = 4500;
            mappanel_width = 575;
            mappanel_height = 575;
            Console.WriteLine("Initialized Map Constants");
        }

        /// <summary>
        /// Function getting a CS:GO Position fetched from a replay file which returns a coordinate for our UI
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static EDM.EDVector3D CSPositionToUIPosition(EDM.EDVector3D p)
        {
            // Calculate a given demo point into a point suitable for our gui minimap: therefore we need a rotation factor, the origin of the coordinate and other data about the map. 
            var x = Math.Abs(map_origin.x - p.x) * (Math.Min(mappanel_width, mapdata_width) / Math.Max(mappanel_width, mapdata_width));
            var y = Math.Abs(map_origin.y - p.y) * (Math.Min(mappanel_height, mapdata_height) / Math.Max(mappanel_height, mapdata_height));
            return new EDM.EDVector3D((float)x, (float)y, p.z);
        }
        #endregion
    }
}
