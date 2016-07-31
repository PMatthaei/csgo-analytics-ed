using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace demojsonparser.src.JSON.objects
{
    class JSONPlayerDetailed : JSONPlayer
    {
        public int HP { get; set; }
        public int armor { get; set; }
        public bool hasHelmet { get; set; }
        public bool hasdefuser { get; set; }
        public bool hasbomb { get; set; }
        public bool isducking { get; set; }
    }
}
