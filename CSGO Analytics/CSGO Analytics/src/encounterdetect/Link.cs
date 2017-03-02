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

    public enum Direction { UNDIRECTED, DEFAULT }; // DEFAULT means Link from actor to reciever, UNDIRECTED means to each other

    public class Link
    {
        private LinkType type;

        private Player[] players;

        private Direction dir;

        private double linkvalue;

        public EDVector3D coll;

        private static int deadcount = 0;

        public Link()
        {

        }

        public Link(Player actor, Player reciever, LinkType type, Direction dir)
        {
            if (actor.getTeam() != reciever.getTeam() && type == LinkType.SUPPORTLINK)
                Console.WriteLine("Cannot create Supportlink between different teams"); // Occurs if a kill occurs where an enemy hit his teammate so hard that he is registered as assister
            if (actor.getTeam() == reciever.getTeam() && type == LinkType.COMBATLINK)
                Console.WriteLine("Cannot create Combatlink in the same team"); //Can occur if teamdamage happens. Dman antimates
            if (actor.isDead() && reciever.isDead())
            {
                deadcount++;
                //throw new Exception("Cannot create link with dead players"); //Can occur if teamdamage happens. Dman antimates
                Console.WriteLine("Deadlinks: "+deadcount);
            }
            players = new Player[2];
            players[0] = actor;
            players[1] = reciever;
            this.type = type;
            this.dir = dir;
        }

        public Link(Player actor, Player reciever, LinkType type, Direction dir, EDVector3D coll)
        {
            if (actor.getTeam() != reciever.getTeam() && type == LinkType.SUPPORTLINK)
                Console.WriteLine("Cannot create Supportlink between different teams"); // Occurs if a kill occurs where an enemy hit his teammate so hard that he is registered as assister
            if (actor.getTeam() == reciever.getTeam() && type == LinkType.COMBATLINK)
                Console.WriteLine("Cannot create Combatlink in the same team"); //Can occur if teamdamage happens. Dman antimates
            if(actor.isDead() && reciever.isDead()) {
                deadcount++;
                throw new Exception("Cannot create link with dead players"); //Can occur if teamdamage happens. Dman antimates
            }
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

        public double getLinkValue()
        {
            return linkvalue;
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
