using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.data.gameobjects;
using CSGO_Analytics.src.math;

namespace CSGO_Analytics.src.data.gameevents

{
    /// <summary>
    /// Nadeevents hold the start and end or explosiontime of a nade.
    /// </summary>
    public class NadeEvents : Event
    {
        public string nadetype { get; set; }
        public EDVector3D position { get; set; }
        
        public override Player[] getPlayers()
        {
            return new Player[] { actor };
        }


        /// <summary>
        /// Radius in which this nade affects players
        /// </summary>
        /// <returns></returns>
        public int getEffectRadius()
        {
            switch (nadetype)
            {
                case "smoke_exploded":
                    return 5;
                case "fire_exploded":
                    return 5;
                case "hegrenade_exploded":
                    return 5;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Tests if a given nadeevent is the ended event of this nadeevent. this just confirms the categorie not that it is indeed the corresponding event.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public bool isEndEventOf(NadeEvents n)
        {
            switch (this.gameevent)
            {
                case "smoke_exploded":
                    return n.gameevent == "smoke_ended";
                case "fire_exploded":
                    return n.gameevent == "firenade_ended";
                case "decoy_exploded":
                    return n.gameevent == "decoy_ended";
                default:
                    return false;
            }
        }

        public override bool Equals(object obj)
        {
            NadeEvents other = obj as NadeEvents;
            if (other == null)
                return false;

            if (this.isEndEventOf(other) && this.actor.Equals(other.actor) && this.nadetype == other.nadetype)
            {
                var dx = Math.Abs(other.position.X - this.position.X);
                var dy = Math.Abs(other.position.Y - this.position.Y);
                if (dx < 10 && dy < 10) // 10 units tolerance to confirm the nade
                    return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + position.X.GetHashCode();
                hash = hash * 23 + position.Y.GetHashCode();
                return hash;
            }
        }
    }

    class FlashNade : NadeEvents
    {
        public IList<PlayerFlashed> flashedplayers { get; set; }

        /// <summary>
        /// Returns true when the last flashed player from the opposing team is not flashed (time == 0) anymore
        /// </summary>
        /// <returns></returns>
        public bool hasFinished()
        {
            foreach (var p in flashedplayers.Where(player => player.getTeam() != actor.getTeam() && !player.isDead()))
            if (p.flashedduration > 0)
                    return true;
            Console.WriteLine("Flash finished");
            return true;
        }
    }
}
