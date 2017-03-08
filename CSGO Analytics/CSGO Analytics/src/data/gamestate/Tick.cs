using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.data.gameevents;
using CSGO_Analytics.src.data.gameobjects;

namespace CSGO_Analytics.src.data.gamestate

{
    public class Tick
    {
        public int tick_id { get; set; }
        public List<Event> tickevents { get; set; }

        /// <summary>
        /// Return all players mentioned in a given tick.
        /// </summary>
        /// <param name="tick"></param>
        /// <returns></returns>
        public List<Player> getUpdatedPlayers() //TODO: what happens if one player is added multiple times
        {
            List<Player> ps = new List<Player>();
            foreach (var g in tickevents)
                ps.AddRange(g.getPlayers()); //Every gameevent provides its acting players
            
            return ps;
        }

        public List<NadeEvents> getNadeEvents()
        {
            return tickevents.Where(t => t.gameevent == "smoke_exploded"  || t.gameevent == "fire_exploded" || t.gameevent == "hegrenade_exploded" || t.gameevent == "flash_exploded").Cast<NadeEvents>().ToList();
        }

        public List<NadeEvents> getNadeEndEvents()
        {
            return tickevents.Where(t => t.gameevent == "smoke_ended" || t.gameevent == "firenade_ended" || t.gameevent == "hegrenade_exploded" || t.gameevent == "flash_exploded").Cast<NadeEvents>().ToList();
        }

        public List<ServerEvents> getServerEvents()
        {
            return tickevents.Where(t => t.gameevent == "player_bind" || t.gameevent == "player_disconnected").Cast<ServerEvents>().ToList();
        }

        public List<Event> getTickevents()
        {
            return tickevents.Where(e => e.gameevent != "player_bind" || e.gameevent != "player_disconnected").ToList();
        }
    }


}
