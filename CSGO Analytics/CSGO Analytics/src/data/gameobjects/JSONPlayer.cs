using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using demojsonparser.src.JSON.objects.subobjects;

namespace demojsonparser.src.JSON.objects
{
    public class JSONPlayer
    {
        public string playername { get; set; }
        public int player_id { get; set; }
        public string team { get; set; }
        public JSONPosition3D position { get; set; }
        public JSONFacing facing { get; set; }
    }
}
