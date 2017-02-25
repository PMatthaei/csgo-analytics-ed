using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Globalization;

namespace CSGO_Analytics.src.data.utils
{
    public class MapMetaData
    {
        public string mapname { get; set; }
        public double mapcenter_x { get; set; }
        public double mapcenter_y { get; set; }
        public double width { get; set; }
        public double height { get; set; }
        public double scale;
        public int rotate { get; set; }
        public double zoom { get; set; }
    }

    public class MapMetaDataPropertyReader
    {
        /// <summary>
        /// Reads a map info file "<mapname>".txt and extracts the relevant data about the map
        /// </summary>
        /// <param name="path"></param>
        public static MapMetaData readProperties(string path)
        {
            string line;

            var fmt = new NumberFormatInfo();
            fmt.NegativeSign = "-";

            MapMetaData metadata = new MapMetaData();
            using (var file = new StreamReader(path))
            {
                while ((line = file.ReadLine()) != null)
                {
                    var resultString = Regex.Match(line, @"-?\d+").Value; //Match negative and positive int numbers

                    if (line.Contains("pos_x"))
                    {
                        metadata.mapcenter_x = double.Parse(resultString, fmt);
                    }
                    else if (line.Contains("pos_y"))
                    {
                        metadata.mapcenter_y = double.Parse(resultString, fmt);
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
                    else if (line.Contains("width"))
                    {
                        metadata.width = double.Parse(resultString, fmt);
                    }
                    else if (line.Contains("height"))
                    {
                        metadata.height = double.Parse(resultString, fmt);
                    }
                }

                file.Close();
            }
            return metadata;
        }
    }
}
