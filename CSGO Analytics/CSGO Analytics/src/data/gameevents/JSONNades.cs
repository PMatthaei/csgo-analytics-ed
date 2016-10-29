using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using demojsonparser.src.JSON.objects;
using demojsonparser.src.JSON.objects.subobjects;

namespace demojsonparser.src.JSON.events
{
    /// <summary>
    /// JSON holding all information needed(supplied) for every nadetype (except flashbangs see JSONFlashNade)
    /// </summary>
    class JSONNade : JSONGameevent
    {
        public string nadetype { get; set; }
        public JSONPlayer thrownby { get; set; }
        public JSONPosition3D position { get; set; }
        
        public override JSONPlayer[] getPlayers()
        {
            return new JSONPlayer[] { thrownby };
        }
    }
}
