using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace demojsonparser.src.JSON.objects
{
    public class JSONRound
    {
        public int round_id { get; set; }
        public string winner { get; set; }
        public List<JSONTick> ticks { get; set; }

    }
}
