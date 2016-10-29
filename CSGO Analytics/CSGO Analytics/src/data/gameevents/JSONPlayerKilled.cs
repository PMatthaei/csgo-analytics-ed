using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using demojsonparser.src.JSON.objects;
using demojsonparser.src.JSON.objects.subobjects;

namespace demojsonparser.src.JSON.events
{
    class JSONPlayerKilled : JSONPlayerHurt
    {
        public JSONPlayerDetailed attacker { get; set; }
        public JSONPlayerDetailed victim { get; set; }
        public bool headhshot { get; set; }
        public int penetrated { get; set; }
        public int hitgroup { get; set; }
        public JSONItem weapon { get; set; }


    }
}
