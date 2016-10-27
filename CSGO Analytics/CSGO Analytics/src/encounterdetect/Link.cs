using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.data.gameobjects;

namespace CSGO_Analytics.src.encounterdetect
{
    enum ComponentType { COMBATLINK, SUPPORTLINK };

    enum Direction { UNDIRECTED, DEFAULT };

    class Link
    {
        private ComponentType type;

        private Player[] players;

        private Direction dir;

        public Link(Player actor, Player reciever, ComponentType type)
        {
            players = new Player[2];
            players[0] = actor;
            players[1] = reciever;
            this.type = type;
        }

        public bool isUndirected()
        {
            return dir == Direction.UNDIRECTED;
        }

    }
}
