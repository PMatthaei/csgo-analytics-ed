using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CS = CSGO_Analytics.src.data.gameobjects;
using CSGO_Analytics.src.json;
using CSGO_Analytics.src.encounterdetect;
using CSGO_Analytics.src.postgres;
using CSGO_Analytics.src.json.jsonobjects;
using CSGO_Analytics.src.json.parser;
using DemoInfoModded;
using Newtonsoft.Json;
using log4net;
using CSGO_Analytics.src.data.gameobjects;
using CSGO_Analytics.src.data.utils;

namespace csgo_analytics_console
{
    class Program
    {
        private const string TEST_PATH = "E:/LRZ Sync+Share/Bacheloarbeit/Demofiles/downloaded valle";
        private const string DUST_ESPORT_PATH = "E:/Demofiles/dust2/";
        private const string PATH = "E:/CS GO Demofiles/";

        private static EncounterDetection ed_algorithm;

        private static List<string> invalidfiles = new List<string>();

        private static ILog LOG;

        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();
            LOG = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            //readFilesFromCommandline(args);
            readAllFiles();
            Console.ReadLine();
        }

        private static void readAllFiles()
        {
            foreach (string file in Directory.EnumerateFiles(PATH, "*.dem"))
            {
                readFile(file);
            }

            foreach (string invalidfile in invalidfiles)
            {
                Console.WriteLine("Could not parse: " + invalidfile);
                Console.WriteLine("Replay not supported yet. Please use only dust2");
            }
        }


        private static void readFilesFromCommandline(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                readFile(args[i]);
            }
        }

        private static bool skipfile = false;


        private static void readFile(string path)
        {
            LOG.Info("Reading: " + Path.GetFileName(path));
            try
            {
                using (var demoparser = new DemoParser(File.OpenRead(path)))
                {
                    ParseTask ptask = new ParseTask
                    {
                        destpath = path,
                        srcpath = path,
                        usepretty = true,
                        showsteps = true,
                        specialevents = true,
                        highdetailplayer = true,
                        positioninterval = 250,
                        settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All, Formatting = Formatting.None }
                    };

                    skipfile = skipFile(demoparser, ptask);
                    LOG.Info("Parsing .dem file");
                    using (var newdemoparser = new DemoParser(File.OpenRead(path)))
                    {
                        if (!skipfile)
                        {
                            GameStateGenerator.GenerateJSONFile(newdemoparser, ptask);

                            using (var reader = new StreamReader(path.Replace(".dem", ".json")))
                            {
                                var deserializedGamestate = Newtonsoft.Json.JsonConvert.DeserializeObject<Gamestate>(reader.ReadToEnd(), ptask.settings);
                                reader.Close();
                                LOG.Info("Loading map meta data");

                                string metapath = @"E:\LRZ Sync+Share\Bacheloarbeit\CS GO Encounter Detection\csgo-stats-ed\CSGO Analytics\CSGO Analytics\src\views\mapviews\" + deserializedGamestate.meta.mapname + ".txt";
                                //string path = @"C:\Users\Patrick\LRZ Sync+Share\Bacheloarbeit\CS GO Encounter Detection\csgo-stats-ed\CSGO Analytics\CSGO Analytics\src\views\mapviews\" + mapname + ".txt";
                                var mapmeta = MapMetaDataPropertyReader.readProperties(metapath);
                                LOG.Info("Detecting Encounters");
                                ed_algorithm = new EncounterDetection(deserializedGamestate, mapmeta);
                                ed_algorithm.detectEncounters();
                            }

                        }
                    }

                    GameStateGenerator.cleanUp();

                }
            }
            catch (Exception e)
            {
                LOG.Error(e.Message);
                LOG.Error(e.StackTrace);
                LOG.Info("Error occured. Skip file: "+ path);
                return;
            }

            if (skipfile)
            {
                invalidfiles.Add(path);
                LOG.Info("Not supported. Skip file: " + path);
                return;
            }
            LOG.Info("----- Parsing and Encounter Detection was sucessful ----- ");

        }

        private static bool skipFile(DemoParser demoparser, ParseTask ptask)
        {
            var mapname = GameStateGenerator.peakMapname(demoparser, ptask);
            Console.WriteLine("Map: " + mapname);
            if (!Map.SUPPORTED_MAPS.Contains(mapname))
                return true;

            GameStateGenerator.cleanUp();
            return false;
        }

        private void readFromDB(string[] args)
        {

            //TODO:
            //Stream s = NPGSQLDelegator.fetchCommandStream("SELECT jsondata->'meta'->'players'->> 2 FROM demodata"); //Hole 3. spieler aus meta array
            //Stream s = NPGSQLDelegator.fetchCommandStream("SELECT jsondata->'match'->'rounds'-> 1 FROM demodata"); //Zweite Runde
            //Stream s = NPGSQLDelegator.fetchCommandStream("SELECT jsondata->'match'->'rounds'-> 1 -> 'ticks' -> 3 FROM demodata"); //Zweite Runde 3. tick
            //Stream s = NPGSQLDelegator.fetchCommandStream("SELECT jsondata->'match'->'rounds'-> ticks' FROM demodata"); //Zweite Runde 3. tick
            Stream s = NPGSQLDelegator.fetchCommandStream("DECLARE js jsonb:= SELECT jsondata->'match'->'rounds' FROM demodata;  i record; BEGIN FOR i IN SELECT* FROM jsonb_each(js) LOOP SELECT i->'ticks'; END LOOP; END;");
            NPGSQLDelegator.fetchCommandStream("SELECT * FROM demodata WHERE jsondata@> '[{\"round_id\": \"1\"}]'");

        }


    }

}
