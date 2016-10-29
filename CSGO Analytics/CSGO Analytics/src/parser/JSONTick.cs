using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using demojsonparser.src.JSON.events;

namespace demojsonparser.src.JSON.objects
{
    public class JSONTick
    {
        public int tick_id { get; set; }
        public List<JSONGameevent> tickevents { get; set; }
    }
}
