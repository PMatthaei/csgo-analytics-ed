using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using demojsonparser.src.JSON.objects;

namespace demojsonparser.src.JSON.events
{
    public class JSONBombState : JSONGameevent
    {
        public JSONPlayer player { get; set; }

        public override JSONPlayer[] getPlayers()
        {
            return new JSONPlayer[] { player };
        }
    }
}
