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
        static void Main(string[] args)
        {

            var path = args[0];
            using ( var demoparser = new DemoParser(File.OpenRead(path)))
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
                using (var reader = new StreamReader(path.Replace(".dem", ".json")))
                {
                    var deserializedGamestate = Newtonsoft.Json.JsonConvert.DeserializeObject<Gamestate>(reader.ReadToEnd(), ptask.settings);
                    EncounterDetectionAlgorithm ed_algorithm = new EncounterDetectionAlgorithm(deserializedGamestate);
                    ed_algorithm.run();
                }
                
            }

            /*
            using (var reader = new StreamReader(File.OpenRead(args[0])))
            {
               var deserializedGamestate = Newtonsoft.Json.JsonConvert.DeserializeObject<JSONGamestate>(reader.ReadLine());
                EncounterDetectionAlgorithm ed_algorithm = new EncounterDetectionAlgorithm(deserializedGamestate);
                ed_algorithm.run();
            }*/

            /*using (var reader = new StreamReader(File.OpenRead(args[0])))
            {
                JSONGamestate deserializedGamestate = Newtonsoft.Json.JsonConvert.DeserializeObject<JSONGamestate>(reader.ReadLine());

                foreach (var r in deserializedGamestate.match.rounds)
                {
                    foreach (var ts in r.ticks)
                    {
                        foreach (var g in ts.tickevents)
                        {
                            Console.WriteLine(g.gameevent);
                        }
                    }
                }

                deserializedGamestate = null;

            }*/

            //Stream s = NPGSQLDelegator.fetchCommandStream("SELECT jsondata->'meta'->'players'->> 2 FROM demodata"); //Hole 3. spieler aus meta array
            //Stream s = NPGSQLDelegator.fetchCommandStream("SELECT jsondata->'match'->'rounds'-> 1 FROM demodata"); //Zweite Runde
            //Stream s = NPGSQLDelegator.fetchCommandStream("SELECT jsondata->'match'->'rounds'-> 1 -> 'ticks' -> 3 FROM demodata"); //Zweite Runde 3. tick
            //Stream s = NPGSQLDelegator.fetchCommandStream("SELECT jsondata->'match'->'rounds'-> ticks' FROM demodata"); //Zweite Runde 3. tick
            //Stream s = NPGSQLDelegator.fetchCommandStream("DECLARE js jsonb:= SELECT jsondata->'match'->'rounds' FROM demodata;  i record; BEGIN FOR i IN SELECT* FROM jsonb_each(js) LOOP SELECT i->'ticks'; END LOOP; END;");
            //NPGSQLDelegator.fetchCommandStream("SELECT * FROM demodata WHERE jsondata@> '[{\"round_id\": \"1\"}]'");

            Console.ReadLine();

        }
    }
}
