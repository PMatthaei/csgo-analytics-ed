using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.encounterdetect.datasource;

namespace CSGO_Analytics.src.json
{
    public class CSGOReplayDeserializer
    {
        private StreamReader reader;

        dynamic deserializedGamestate;

        /// <summary>
        /// Deserializes CSGO JSON replay data
        /// </summary>
        /// <param name="jsonpath"></param>
        public CSGOReplayDeserializer(string jsonpath)
        {
            reader = new StreamReader(File.OpenRead(jsonpath));
            deserializedGamestate = Newtonsoft.Json.JsonConvert.DeserializeObject(reader.ReadLine());
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
            var mapname = deserializedGamestate.meta.mapname;
            var tickrate = deserializedGamestate.meta.tickrate;
            var players = deserializedGamestate.meta.players;
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

        /// <summary>
        /// Returns a list of all ticksas dynamic objects
        /// </summary>
        /// <param name="rounds"></param>
        /// <returns></returns>
        public List<Tick> deserializeTicks()
        {
            List<Tick> ticks = new List<Tick>();

            foreach (var round in deserializedGamestate.match.rounds)
            {
                foreach (var tick in round.ticks)
                {
                    ticks.Add(new Tick(tick.tick_id, tick.tickevents));
                }
            }
            return ticks;
        }

        public Tick deserializeRound()
        {
            dynamic round = null;
            foreach (var tick in round.ticks)//all ticks in a round
            {

            }
            return null;
        }
    }
}
