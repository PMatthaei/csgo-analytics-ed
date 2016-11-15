using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace CSGO_Analytics.src.utils
{
    class MapDataPropertyReader
    {
        public string mapname;
        public int mapcenter_x;
        public int mapcenter_y;
        public double scale;
        public int rotate;
        public double zoom;

        /// <summary>
        /// Reads a map info file "<mapname>".txt and extracts the relevant data about the map
        /// </summary>
        /// <param name="path"></param>
        public MapDataPropertyReader(string path)
        {
            string line;

            StreamReader file = new StreamReader(path);
            while ((line = file.ReadLine()) != null)
            {
                var resultString = Regex.Match(line, @"\d+").Value;

                if (line.Contains("pos_x"))
                {
                    mapcenter_x = Int32.Parse(resultString);
                }
                else if (line.Contains("pos_y"))
                {
                    mapcenter_y = Int32.Parse(resultString);
                }
                else if (line.Contains("scale"))
                {
                    scale = Double.Parse(resultString);
                }
                else if (line.Contains("rotate"))
                {
                    rotate = Int32.Parse(resultString);
                }
                else if (line.Contains("zoom"))
                {
                    zoom = Double.Parse(resultString);
                }
            }

            file.Close();
        }
    }
}
