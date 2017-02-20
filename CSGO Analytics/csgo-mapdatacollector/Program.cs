using System;
using System.IO;
using System.Collections.Generic;
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

        private const string PATH = "E:/LRZ Sync+Share/Bacheloarbeit/Demofiles/downloaded valle/";

        private static string mapname;

        private static JsonSerializerSettings settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            Formatting = Formatting.None
        };

        static int count;

        private static void readAllFiles()
        {
            foreach (string file in Directory.EnumerateFiles(PATH, "*.dem"))
            {
                var data = readFile(file);
                var path = PATH + mapname + ".json";
                if (!File.Exists(path))
                    createMapdataFile(path, data);
                else
                    AddDataToFile(path, data);
                count++;
            }

        }

        private static int tickcount = 0;
        private static HashSet<EDVector3D> readFile(string path)
        {
            Console.WriteLine("Reading: " + Path.GetFileName(path));
            HashSet<EDVector3D> mappositions = new HashSet<EDVector3D>();

            using (var parser = new DemoParser(File.OpenRead(path)))
            {
                parser.ParseHeader();

                mapname = parser.Map;
                Console.WriteLine("Map: " + mapname);

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
                }
            }

            Console.WriteLine("Positions found in this file: " + mappositions.Count + "\n");

            return mappositions;

        }

        private static void createMapdataFile(string path, HashSet<EDVector3D> mappos)
        {
            string json = JsonConvert.SerializeObject(mappos, settings);
            using (var outputStream = new StreamWriter(path))
            {
                outputStream.Write(json);
                outputStream.Close();
            }

        }

        private static void AddDataToFile(string path, HashSet<EDVector3D> mappos)
        {
            HashSet<EDVector3D> deserializedmapdata;
            using (var reader = new StreamReader(path))
            {
                deserializedmapdata = JsonConvert.DeserializeObject<HashSet<EDVector3D>>(reader.ReadToEnd(), settings);
            }
            Console.WriteLine("Positions found before adding new ones: " + deserializedmapdata.Count + "\n");
            HashSet<EDVector3D> newdata = new HashSet<EDVector3D>(deserializedmapdata.Union(mappos));
            Console.WriteLine("Positions found after adding new ones: " + newdata.Count() + "\n");
            createMapdataFile(path, newdata);
        }
    }
}
