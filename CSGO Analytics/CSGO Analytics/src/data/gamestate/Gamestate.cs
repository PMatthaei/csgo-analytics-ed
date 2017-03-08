using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.data.gameobjects;

namespace CSGO_Analytics.src.data.gamestate
{
    public class Gamestate
    {
        public Meta meta { get; set; }
        public Match match { get; set; }
    }

    public class Meta
    {
        public int gamestate_id { get; set; }
        public string mapname { get; set; }
        public float tickrate { get; set; }
        public int tickcount { get; set; }
        public List<PlayerDetailed> players { get; set; }
    }

    public class Match
    {
        public Team winnerteam { get; set; }
        public List<Round> rounds { get; set; }
    }

    public class Round
    {
        public int round_id { get; set; }
        public string winner { get; set; }
        public List<Tick> ticks { get; set; }

        /// <summary>
        /// Get range of ticks in a round. Cannot be used for time measure of this round as empty ticks are dissmissed in between!
        /// </summary>
        /// <returns></returns>
        public int getRoundTickRange()
        {
            var mintick = ticks.Min(tick => tick.tick_id);
            var maxtick = ticks.Max(tick => tick.tick_id);

            return (maxtick - mintick);
        }
    }
}
