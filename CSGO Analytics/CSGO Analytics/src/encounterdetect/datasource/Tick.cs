using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.data.gameobjects;

namespace CSGO_Analytics.src.encounterdetect.datasource
{
    public class Tick : IDisposable // Maybe a superclass tickobject (use it for gameevent, tick, combatcomponent, encounter, link
    {
        /// <summary>
        /// Id of this tick
        /// </summary>
        public int tick_id { get; set; }

        public List<GameEvent> gameevents { get; set; }
        public List<Player> players { get; set; }

        public Tick(int tick_id, dynamic dGameevents)
        {
            this.tick_id = tick_id;
            this.gameevents = new List<GameEvent>();
            foreach (var e in dGameevents)
            {
                gameevents.Add(GameEvent.build(e));
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
            return players;
        }

        /// <summary>
        /// Returns all players spotted in this tick
        /// </summary>
        /// <returns></returns>
        public List<Player> getSpottedPlayers()
        {
            return players;
        }

        public void Dispose()
        {
            gameevents.Clear();
            gameevents = null;
            players.Clear();
            players = null;
            tick_id = -1;
        }
    }
}
