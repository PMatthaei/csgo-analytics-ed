using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_Analytics.src.json.jsonobjects
{
    public class JSONRound
    {
        public int round_id { get; set; }
        public string winner { get; set; }
        public List<Tick> ticks { get; set; }

    }
}
