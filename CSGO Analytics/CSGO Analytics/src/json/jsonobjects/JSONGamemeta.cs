using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.data.gameobjects;

namespace CSGO_Analytics.src.json.jsonobjects
{
    public class JSONGamemeta
    {
        public int gamestate_id { get; set; }
        public string mapname { get; set; }
        public float tickrate { get; set; }
        public int tickcount { get; set; }
        public List<PlayerMeta> players { get; set; }
    }
}
