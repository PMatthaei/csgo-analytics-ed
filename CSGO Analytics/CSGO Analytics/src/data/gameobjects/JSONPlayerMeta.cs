using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace demojsonparser.src.JSON.objects
{
    public class JSONPlayerMeta
    {
        public string playername { get; set; }
        public int player_id { get; set; }
        public string team { get; set; }
        public string clanname { get; set; }
        public long steam_id { get; set; }
    }
}
