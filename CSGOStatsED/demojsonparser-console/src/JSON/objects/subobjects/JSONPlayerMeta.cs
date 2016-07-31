using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace demojsonparser.src.JSON.objects
{
    class JSONPlayerMeta : JSONPlayer
    {
        public string clanname { get; set; }
        public long steam_id { get; set; }
    }
}
