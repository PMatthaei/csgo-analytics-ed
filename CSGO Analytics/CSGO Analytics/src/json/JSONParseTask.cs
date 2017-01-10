using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DemoInfoModded;
using Newtonsoft.Json;

namespace CSGO_Analytics.src.json.parser
{
    /// <summary>
    /// Class holding every data on how and where to parse.
    /// </summary>
    public class ParseTask
    {
        public string srcpath { get; set; }

        public string destpath { get; set; }

        public bool usepretty { get; set; }

        public bool showsteps { get; set; }

        public int positioninterval { get; set; }

        public bool specialevents { get; set; }

        public bool highdetailplayer { get; set; }

        public JsonSerializerSettings settings { get; set; }

    }
}
