using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.data.gameobjects;
using CSGO_Analytics.src.math;

namespace CSGO_Analytics.src.encounterdetect
{
    public enum LinkType { COMBATLINK, SUPPORTLINK };

    public enum Direction { UNDIRECTED, DEFAULT }; // DEFAULT means Link is directed from actor to reciever, UNDIRECTED means to each other

    public class Link
    {
        /// <summary>
        /// Type of the Link
        /// </summary>
        private LinkType type;

        /// <summary>
        /// Players contained in this Link. Use getActor() and getReciever().
        /// </summary>
        private Player[] players;

        /// <summary>
        /// Direction of this Link
        /// </summary>
        private Direction dir;

        /// <summary>
        /// Impact dont by this link. Can either be the amount of damage or heal or buff etc
        /// </summary>
        public double impact { get; set; }

        /// <summary>
        /// Defines if this link was built with a kill event
        /// </summary>
        public bool isKill { get; set; }

        public EDVector3D coll;

        public Link()
        {

        }

        public Link(Player actor, Player reciever, LinkType type, Direction dir)
        {
            if (actor == null || reciever == null) throw new Exception("Players cannot be null");
            if (actor.getTeam() != reciever.getTeam() && type == LinkType.SUPPORTLINK)
            {
                Console.WriteLine("Cannot create Supportlink between different teams"); // Occurs if a kill occurs where an enemy hit his teammate so hard that he is registered as assister
            }
            if (actor.getTeam() == reciever.getTeam() && type == LinkType.COMBATLINK)
            {
                Console.WriteLine("Cannot create Combatlink in the same team"); //Can occur if teamdamage happens. Dman antimates
            }

            players = new Player[2];
            players[0] = actor;
            players[1] = reciever;
            this.type = type;
            this.dir = dir;
        }

        public Link(Player actor, Player reciever, LinkType type, Direction dir, EDVector3D coll)
        {
            if (actor == null || reciever == null) throw new Exception("Players cannot be null");
            if (actor.getTeam() != reciever.getTeam() && type == LinkType.SUPPORTLINK)
                Console.WriteLine("Cannot create Supportlink between different teams"); // Occurs if a kill occurs where an enemy hit his teammate so hard that he is registered as assister
            if (actor.getTeam() == reciever.getTeam() && type == LinkType.COMBATLINK)
                Console.WriteLine("Cannot create Combatlink in the same team"); //Can occur if teamdamage happens. Dman antimates

            players = new Player[2];
            players[0] = actor;
            players[1] = reciever;
            this.type = type;
            this.dir = dir;
            this.coll = coll;
        }

        public bool isUndirected()
        {
            return dir == Direction.UNDIRECTED;
        }

        public Player getActor()
        {
            return players[0];
        }

        public Player getReciever()
        {
            return players[1];
        }

        public LinkType getLinkType()
        {
            return type;
        }

        public double getImpact()
        {
            return impact;
        }

        override public string ToString()
        {
            return type.ToString() + " | Actor: " + players[0].playername + "- Reciever: " + players[1].playername;
        }

        override public bool Equals(object other)
        {
            var link = other as Link;
            if (link == null)
                return false;
            if (getActor().Equals(link.getActor()) && getReciever().Equals(link.getReciever()) && dir == link.dir)
                return true;

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
