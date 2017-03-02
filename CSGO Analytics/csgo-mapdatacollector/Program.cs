using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DemoInfoModded;
using CSGO_Analytics.src.math;
using Newtonsoft.Json;

namespace csgo_mapdatacollector
{
    class Program
    {
        static void Main(string[] args)
        {
            readAllFiles();
            Console.ReadLine();
        }

        private const string PATH = "E:/LRZ Sync+Share/Demofiles/";

        private static string mapname;

        private static Dictionary<string, double> all_maps = new Dictionary<string, double>();

        private const string target_map = "de_inferno";

        private static JsonSerializerSettings settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            Formatting = Formatting.None
        };

        static double count;

        private static void readAllFiles()
        {
            foreach (string file in Directory.EnumerateFiles(PATH, "*.dem"))
            {
                readDemoFile(file);
                count++;
            }
            Console.WriteLine(target_map + " occured: " + target_map_count);
            foreach(var mapname in all_maps)
                Console.WriteLine(mapname.Key +": "+ mapname.Value+"/"+count + " "+ (int)((mapname.Value / count) * 100)+ "%");

        }
        private static int target_map_count = 0;
        private static int tickcount = 0;
        private static void readDemoFile(string path)
        {
            Console.WriteLine("Reading: " + Path.GetFileName(path));
            HashSet<EDVector3D> mappositions = new HashSet<EDVector3D>();
            Hashtable hurtpairs = new Hashtable();

            using (var parser = new DemoParser(File.OpenRead(path)))
            {
                parser.ParseHeader();

                mapname = parser.Map;
                mapname.Trim();
                if (!all_maps.ContainsKey(mapname))
                    all_maps.Add(mapname, 0);
                else
                    all_maps[mapname]++;


                Console.WriteLine("Map: " + mapname);
                if (mapname == target_map) target_map_count++;

                parser.PlayerKilled += (object sender, PlayerKilledEventArgs e) =>
                {
                    if (e.Killer == null || e.Victim == null) return;
                    var attackerpos = new EDVector3D((float)Math.Round(e.Killer.Position.X), (float)Math.Round(e.Killer.Position.Y), (float)Math.Round(e.Killer.Position.Z)).ResetZ();
                    var victimpos = new EDVector3D((float)Math.Round(e.Victim.Position.X), (float)Math.Round(e.Victim.Position.Y), (float)Math.Round(e.Victim.Position.Z)).ResetZ();

                    hurtpairs[attackerpos] = victimpos;
                };

                parser.PlayerHurt += (object sender, PlayerHurtEventArgs e) =>
                {
                    if (e.Attacker == null || e.Victim == null) return;
                    var attackerpos = new EDVector3D((float)Math.Round(e.Attacker.Position.X), (float)Math.Round(e.Attacker.Position.Y), (float)Math.Round(e.Attacker.Position.Z)).ResetZ();
                    var victimpos = new EDVector3D((float)Math.Round(e.Victim.Position.X), (float)Math.Round(e.Victim.Position.Y), (float)Math.Round(e.Victim.Position.Z)).ResetZ();

                    hurtpairs[attackerpos] = victimpos;
                };

                parser.TickDone += (sender, e) =>
                {
                    var updaterate = 8 * ((int)Math.Ceiling(parser.TickRate) / 32);
                    // Dump playerpositions every at a given updaterate according to the tickrate
                    if ((tickcount % updaterate == 0))
                        foreach (var player in parser.PlayingParticipants)
                        {
                            if (player.Velocity.Z == 0)
                                mappositions.Add(new EDVector3D((float)Math.Round(player.Position.X), (float)Math.Round(player.Position.Y), (float)Math.Round(player.Position.Z)));
                        }

                    tickcount++;

                };

                //
                // MAIN PARSING LOOP
                //
                try
                {
                    //Parse tickwise and add the resulting tick to the round object
                    while (parser.ParseNextTick())
                    {


                    }

                }
                catch (System.IO.EndOfStreamException e)
                {
                    Console.WriteLine("Problem with tick-parsing. Is your .dem valid? See this projects github page for more info.\n");
                    Console.WriteLine("Stacktrace: " + e.StackTrace + "\n");
                    Console.ReadLine();
                }
            }

            Console.WriteLine("Positions found in this file: " + mappositions.Count + "\n");
            Console.WriteLine("Hurtevents found in this file: " + hurtpairs.Count + "\n");

            var pospath = PATH + mapname + "_positions.json";
            if (!File.Exists(pospath))
                writeMapdataFile(pospath, mappositions);
            else
                AddMapData(pospath, mappositions);

            var hurtpath = PATH + mapname + "_hurtevents.json";
            if (!File.Exists(hurtpath))
                writeHurtdataFile(hurtpath, hurtpairs);
            else
                AddHurtData(hurtpath, hurtpairs);

        }


        private static void writeHurtdataFile(string hurtpath, Hashtable hurtpairs)
        {
            string json = JsonConvert.SerializeObject(hurtpairs, settings);
            using (var outputStream = new StreamWriter(hurtpath))
            {
                outputStream.Write(json);
                outputStream.Close();
            }
        }

        private static void AddHurtData(string hurtpath, Hashtable hurtpairs)
        {
            Hashtable deserializedmapdata;
            using (var reader = new StreamReader(hurtpath))
            {
                deserializedmapdata = JsonConvert.DeserializeObject<Hashtable>(reader.ReadToEnd(), settings);
            }
            Console.WriteLine("Hurtevents found before adding new ones: " + deserializedmapdata.Count + "\n");
            foreach(var key in hurtpairs.Keys)
            {
                var vic = (EDVector3D)hurtpairs[key];
                var att = (EDVector3D)key;
                deserializedmapdata.Add(att, vic);
            }
            Console.WriteLine("Hurtevents found after adding new ones: " + deserializedmapdata.Count + "\n");
            writeHurtdataFile(hurtpath, deserializedmapdata);
        }

        private static void writeMapdataFile(string path, HashSet<EDVector3D> mappos)
        {
            string json = JsonConvert.SerializeObject(mappos, settings);
            using (var outputStream = new StreamWriter(path))
            {
                outputStream.Write(json);
                outputStream.Close();
            }
        }

        private static void AddMapData(string path, HashSet<EDVector3D> mappos)
        {
            HashSet<EDVector3D> deserializedmapdata;
            using (var reader = new StreamReader(path))
            {
                deserializedmapdata = JsonConvert.DeserializeObject<HashSet<EDVector3D>>(reader.ReadToEnd(), settings);
            }
            Console.WriteLine("Positions found before adding new ones: " + deserializedmapdata.Count + "\n");
            HashSet<EDVector3D> newdata = new HashSet<EDVector3D>(deserializedmapdata.Union(mappos));
            Console.WriteLine("Positions found after adding new ones: " + newdata.Count() + "\n");
            writeMapdataFile(path, newdata);
        }
    }
}
