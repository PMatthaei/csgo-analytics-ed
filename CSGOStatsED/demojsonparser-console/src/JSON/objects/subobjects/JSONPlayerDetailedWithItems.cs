using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using demojsonparser.src.JSON.objects.subobjects;

namespace demojsonparser.src.JSON.objects
{
    class JSONPlayerDetailedWithItems : JSONPlayerDetailed
    {
        public List<JSONItem> items { get; set; }
    }
}
