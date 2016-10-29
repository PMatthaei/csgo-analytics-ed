using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using demojsonparser.src.JSON.objects;

namespace demojsonparser.src.JSON.events
{
    public class JSONGameevent
    {
        public string gameevent { get; set; }

        public virtual JSONPlayer[] getPlayers() { return null; }

    }
}
