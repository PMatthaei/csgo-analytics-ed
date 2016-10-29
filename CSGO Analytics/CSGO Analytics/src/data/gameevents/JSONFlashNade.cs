using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using demojsonparser.src.JSON.objects;
using demojsonparser.src.JSON.objects.subobjects;

namespace demojsonparser.src.JSON.events
{
    class JSONFlashNade : JSONNade
    {
        public IList<JSONPlayerFlashed> flashedplayers { get; set; }
    }
}
