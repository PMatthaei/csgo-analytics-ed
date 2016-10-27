using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_Analytics.src.json
{
    public class CSGOReplayDeserializer
    {
        private StreamReader reader;

        /// <summary>
        /// Deserializes CSGO JSON replay data
        /// </summary>
        /// <param name="jsonpath"></param>
        public CSGOReplayDeserializer(string jsonpath)
        {
            using (var stream = File.OpenRead(jsonpath)) //TODO change to DB reader or DB String
            {
                reader = new StreamReader(stream);
            }

        }

        /// <summary>
        /// Returns an dynamic object of the jsonstring.
        /// </summary>
        /// <param name="jsonString"></param>
        /// <returns></returns>
        public static dynamic deserializeJSONString(string jsonString)
        {
            dynamic deserializedObject = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonString); //Runtime!!
            return deserializedObject;
        }

        /// <summary>
        /// Returns the gamestateobject from a gamestate-jsonstring ( ONE-LINER!! NO PRETTY JSONs)
        /// </summary>
        /// <param name="jsonString"></param>
        /// <returns></returns>
        public dynamic deserializeGamestate()
        {
            dynamic deserializedGamestate = Newtonsoft.Json.JsonConvert.DeserializeObject(reader.ReadLine()); //Runtime!! TODO: make known(non-dynamic objects of it?)
            var mapname = deserializedGamestate.match.mapname;
            var tickrate = deserializedGamestate.match.tickrate;
            var players = deserializedGamestate.match.players;
            foreach (var round in deserializedGamestate.match.rounds) //all rounds
            {
                var round_id = round.round_id;

                foreach (var tick in round.ticks)//all ticks in a round
                {
                    var tick_id = tick.tick_id;

                    foreach (var e in tick.tickevents)//all events in a tick
                    {
                        var gameeventtype = e.gamevent;
                    }
                }
            }

            return null; //TODO object holding the non dynamic gamestate
        }
    }
}
