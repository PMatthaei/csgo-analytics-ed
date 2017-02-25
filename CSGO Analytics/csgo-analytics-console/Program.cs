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

namespace csgo_analytics_console
{
    class Program
    {
        private const string TEST_PATH = "E:/LRZ Sync+Share/Bacheloarbeit/Demofiles/downloaded valle";
        private const string DUST_ESPORT_PATH = "E:/Demofiles/dust2/";
        private const string PATH = "E:/Demofiles/";

        private static EncounterDetection ed_algorithm;

        private static List<string> invalidfiles = new List<string>();

        static void Main(string[] args)
        {
            readFilesFromCommandline(args);
            //readAllFiles();
            Console.ReadLine();
        }

        private static void readAllFiles()
        {
            foreach (string file in Directory.EnumerateFiles(TEST_PATH, "*.dem"))
            {
                readFile(file);
            }

            foreach (string invalidfile in invalidfiles)
            {
                System.IO.File.Move(invalidfile, invalidfile.Replace(".dem", "_nondust2.dem"));
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

        private static void readFile(string path)
        {
            Console.WriteLine("Reading: " + Path.GetFileName(path));

            var skipfile = false;
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

                var newdemoparser = new DemoParser(File.OpenRead(path));
                if (!skipfile)
                {
                    GameStateGenerator.GenerateJSONFile(newdemoparser, ptask);

                    using (var reader = new StreamReader(path.Replace(".dem", ".json")))
                    {
                        var deserializedGamestate = Newtonsoft.Json.JsonConvert.DeserializeObject<Gamestate>(reader.ReadToEnd(), ptask.settings);
                        reader.Close();
                        try
                        {
                            ed_algorithm = new EncounterDetection(deserializedGamestate);
                            ed_algorithm.detectEncounters();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            return;
                        }
                    }

                }
                GameStateGenerator.cleanUp();

            }

            if (skipfile)
            {
                invalidfiles.Add(path);
                Console.WriteLine("Skip file. Check for invalidity");
            }

        }

        private static bool skipFile(DemoParser demoparser, ParseTask ptask)
        {
            var mapname = GameStateGenerator.peakMapname(demoparser, ptask);
            Console.WriteLine("Map: " + mapname);
            if (mapname != "de_dust2")
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
