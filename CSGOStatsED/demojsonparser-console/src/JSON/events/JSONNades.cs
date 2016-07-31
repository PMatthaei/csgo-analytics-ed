using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using demojsonparser.src.JSON.objects;
using demojsonparser.src.JSON.objects.subobjects;

namespace demojsonparser.src.JSON.events
{
    class JSONNades : JSONGameevent
    {
        public string nadetype { get; set; }
        public JSONPlayer thrownby { get; set; }
        public JSONPosition3D position { get; set; }
        public IList<JSONPlayer> flashedplayers { get; set; }
    }
}
