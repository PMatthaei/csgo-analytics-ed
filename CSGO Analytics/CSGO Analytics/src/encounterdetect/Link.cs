using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.data.gameobjects;

namespace CSGO_Analytics.src.encounterdetect
{
    public enum ComponentType { COMBATLINK, SUPPORTLINK };

    public enum Direction { UNDIRECTED, DEFAULT }; //DEFAULT means Link from actor to reciever, UNDIRECTED means both to each other

    public class Link
    {
        private ComponentType type;

        private Player[] players;

        private Direction dir;

        public Link()
        {

        }

        public Link(Player actor, Player reciever, ComponentType type, Direction dir)
        {
            players = new Player[2];
            players[0] = actor;
            players[1] = reciever;
            this.type = type;
            this.dir = dir;
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
