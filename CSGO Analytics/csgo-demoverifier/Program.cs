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
using log4net;
using CSGO_Analytics.src.data.gameobjects;

namespace csgo_demoverifier
{
    class Program
    {
        private const string TEST_PATH = "E:/LRZ Sync+Share/Bacheloarbeit/Demofiles/downloaded valle";
        private const string DUST_ESPORT_PATH = "E:/Demofiles/dust2/";
        private const string PATH = "E:/Demofiles/";

        private static ILog LOG;

        private static bool invalid;

        private static int valids_count = 0;

        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();
            LOG = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            readAllFiles();
            Console.ReadLine();
        }

        private static void readAllFiles()
        {
            foreach (string file in Directory.EnumerateFiles(TEST_PATH, "*.dem"))
            {
                invalid = false;
                readFile(file);
            }
            LOG.Info("----- Valid demos: " + valids_count + " ----- ");
            LOG.Info("----- Task ended ----- ");

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
            LOG.Info("Reading: " + Path.GetFileName(path));
            try
            {
                var count = 0;
                using (var demoparser = new DemoParser(File.OpenRead(path)))
                {
                    demoparser.ParseHeader();
                    LOG.Info("Map: " + demoparser.Map);
                    var track = false;
                    demoparser.MatchStarted += (object sender, MatchStartedEventArgs e) =>
                    {
                        track = true;
                    };
                    demoparser.PlayerDisconnect += (object sender, PlayerDisconnectEventArgs e) =>
                    {
                        if (track) count++;
                    };
                    demoparser.PlayerBind += (object sender, PlayerBindEventArgs e) =>
                    {
                        if (track) count++;
                    };
                    demoparser.ParseToEnd();
                }
                if (count == 0)
                    valids_count++;
                else
                    LOG.Info("Invalid Events: " + count);

            }
            catch (Exception e)
            {
                LOG.Error(e.Message);
                LOG.Error(e.StackTrace);
                LOG.Info("Error occured. Skip file: " + path);
                return;
            }
        }

    }

}