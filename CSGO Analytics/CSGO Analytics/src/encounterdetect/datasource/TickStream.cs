using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.data.gameobjects;
using CSGO_Analytics.src.json;

namespace CSGO_Analytics.src.encounterdetect
{


    class TickStream : MemoryStream, IDisposable //TODO: is this useful? if so -> implement it
    {
        public Tick readTick()
        {
            using (StreamReader reader = new StreamReader(this))
            {
                string tickstring = reader.ReadLine(); //Correct position?
                dynamic deserializedTick = CSGOReplayDeserializer.deserializeJSONString(tickstring);

                this.Position = this.Position + 1;
                return new Tick(deserializedTick.tick_id, deserializedTick.gameevents);
            }
        }

        public bool hasNextTick()
        {
            if(this.Position < this.Length)
                return true;

            return false;
        }
    }

    class Tick : IDisposable // Maybe a superclass tickobject (use it for gameevent, tick, combatcomponent, encounter, link
    {
        /// <summary>
        /// Id of this tick
        /// </summary>
        public int tick_id;

        public List<GameEvent> gameevents;

        public Tick(int tick_id, dynamic dGameevents)
        {
            this.tick_id = tick_id;
            this.gameevents = new List<GameEvent>();
            foreach (var e in dGameevents)
            {
                Console.WriteLine(e.gameevent);
                Console.WriteLine(e.player.position.x);
            }
        }

        /// <summary>
        /// All gameevents happend in that tick
        /// </summary>
        /// <returns></returns>
        public List<GameEvent> getGameEvents()
        {
            return gameevents;
        }

        /// <summary>
        /// Returns all players mentioned in this tick
        /// </summary>
        /// <returns></returns>
        public List<Player> getUpdatedPlayers()
        {
            return null;
        }

        public void Dispose()
        {
            gameevents.Clear();
            gameevents = null;
            tick_id = -1;
        }
    }
}
