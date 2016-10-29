using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.encounterdetect.datasource;
using demojsonparser.src.JSON.objects;

namespace CSGO_Analytics.src.json
{
    public class CSGOReplayDeserializer
    {

        private JSONGamestate deserializedGamestate;

        /// <summary>
        /// Deserializes CSGO JSON replay data
        /// </summary>
        /// <param name="jsonpath"></param>
        public CSGOReplayDeserializer(string jsonpath)
        {
            using (var reader = new StreamReader(File.OpenRead(jsonpath)))
            {
                deserializedGamestate = Newtonsoft.Json.JsonConvert.DeserializeObject<JSONGamestate>(reader.ReadLine());
            }
        }

        public JSONGamestate getGamestate()
        {
            return deserializedGamestate;
        }

        /// <summary>
        /// Returns a list of all ticks
        /// </summary>
        /// <param name="rounds"></param>
        /// <returns></returns>
        public List<JSONTick> getTicks()
        {
            List<JSONTick> ticks = new List<JSONTick>();

            foreach (var r in deserializedGamestate.match.rounds)
            {
                ticks.AddRange(r.ticks);
            }
            return ticks;
        }

    }
}
