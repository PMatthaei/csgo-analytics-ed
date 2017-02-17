using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.data.gameobjects;

namespace CSGO_Analytics.src.json.jsonobjects
{
    public class Gamestate
    {
        public GamestateMeta meta { get; set; }
        public Match match { get; set; }
    }

    public class GamestateMeta
    {
        public int gamestate_id { get; set; }
        public string mapname { get; set; }
        public float tickrate { get; set; }
        public int tickcount { get; set; }
        public List<PlayerDetailed> players { get; set; }
    }

    public class Match
    {
        public List<Round> rounds { get; set; }
    }

    public class Round
    {
        public int round_id { get; set; }
        public string winner { get; set; }
        public List<Tick> ticks { get; set; }

    }
}
