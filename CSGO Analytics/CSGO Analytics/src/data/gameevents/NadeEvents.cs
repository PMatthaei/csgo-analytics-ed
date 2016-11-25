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
        public Vector position { get; set; }
        
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
                default:
                    return 0;
            }
        }

        public bool isEndEvent(NadeEvents n)
        {
            switch (gameevent)
            {
                case "smoke_exploded":
                    return n.gameevent == "smoke_ended";
                case "fire_exploded":
                    return n.gameevent == "firenade_ended";
                default:
                    return false;
            }
        }
        /*
        public override bool Equals(object obj)
        {
            NadeEvents other = obj as NadeEvents;
            if (other == null)
            {
                Console.WriteLine("Nade null");
                return false;
            }

            if (other.gameevent == this.gameevent && other.actor == this.actor && other.nadetype == this.nadetype)
            {
                 var dx = Math.Abs(other.position.x - this.position.x);
                var dy = Math.Abs(other.position.y - this.position.y);
                if (dx < 20 && dy < 20)
                    Console.WriteLine("Nade test true");
                    return true;
            }
            Console.WriteLine("Nade test false");
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + position.x.GetHashCode();
                hash = hash * 23 + position.y.GetHashCode();
                return hash;
            }
        }*/
    }

    class FlashNade : NadeEvents
    {
        public IList<PlayerFlashed> flashedplayers { get; set; }
    }
}
