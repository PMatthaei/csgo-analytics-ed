using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace CSGO_Analytics.src.utils
{
    public class MapMetaData
    {
        public string mapname { get; set; }
        public int mapcenter_x { get; set; }
        public int mapcenter_y { get; set; }
        public double scale;
        public int rotate { get; set; }
        public double zoom { get; set; }
    }

    public class MapMetaDataPropertyReader
    {
        public MapMetaData metadata;

        /// <summary>
        /// Reads a map info file "<mapname>".txt and extracts the relevant data about the map
        /// </summary>
        /// <param name="path"></param>
        public MapMetaDataPropertyReader(string path)
        {
            string line;

            StreamReader file = new StreamReader(path);
            while ((line = file.ReadLine()) != null)
            {
                var resultString = Regex.Match(line, @"\d+").Value;

                if (line.Contains("pos_x"))
                {
                    metadata.mapcenter_x = Int32.Parse(resultString);
                }
                else if (line.Contains("pos_y"))
                {
                    metadata.mapcenter_y = Int32.Parse(resultString);
                }
                else if (line.Contains("scale"))
                {
                    metadata.scale = Double.Parse(resultString);
                }
                else if (line.Contains("rotate"))
                {
                    metadata.rotate = Int32.Parse(resultString);
                }
                else if (line.Contains("zoom"))
                {
                    metadata.zoom = Double.Parse(resultString);
                }
            }

            file.Close();
        }
    }
}
