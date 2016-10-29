using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace demojsonparser.src.JSON.objects
{
    public class JSONGamemeta
    {
        public int gamestate_id { get; set; }
        public string mapname { get; set; }
        public float tickrate { get; set; }
        public List<JSONPlayerMeta> players { get; set; }
    }
}
