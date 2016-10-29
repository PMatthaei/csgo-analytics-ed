using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using demojsonparser.src.JSON.objects;
using demojsonparser.src.JSON.objects.subobjects;

namespace demojsonparser.src.JSON.events
{
    class JSONPlayerHurt : JSONGameevent
    {
        public JSONPlayer attacker { get; set; }
        public JSONPlayer victim { get; set; }
        public int HP { get; set; }
        public int armor { get; set; }
        public int armor_damage { get; set; }
        public int HP_damage { get; set; }
        public string hitgroup { get; set; }
        public JSONItem weapon { get; set; }

        public override JSONPlayer[] getPlayers()
        {
            return new JSONPlayer[]{ attacker, victim };
        }
    }
}
