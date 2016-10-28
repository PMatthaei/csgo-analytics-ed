using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.data.gameobjects;
using CSGO_Analytics.src.json;
using CSGO_Analytics.src.encounterdetect;
using CSGO_Analytics.src.postgres;
using CSGO_Analytics.src.encounterdetect.datasource;

namespace csgo_analytics_console
{
    class Program
    {
        static void Main(string[] args)
        {

            /*CSGOReplayDeserializer cs = new CSGOReplayDeserializer(args[0]);
            foreach (var tick in cs.deserializeTicks())
            {
                Console.WriteLine(tick.tick_id);

            }*/
            List<GameEvent> gs = new List<GameEvent>();
            gs.Add(new PlayerSpotted());
            gs.Add(new PlayerKilledEvent());
            gs.Add(new WeaponFireEvent());

            foreach(var g in gs)
            {
                g.createLink();
            }
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
