using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;
using CSGO_Analytics.src.data.gameobjects;
using CSGO_Analytics.src.encounterdetect;

namespace CSGO_Analytics.src.data.gameevents
{

    class WeaponFire : Gameevent
    {
        public Weapon weapon { get; set; }


        public override Player[] getPlayers()
        {
            return new Player[] { actor };
        }

    }

    class PlayerSpotted : Gameevent
    {
        public Player spotter { get; set; } //TODO: how find out spotter? or do this in algorithm?

        public override Player[] getPlayers()
        {
            return new Player[] { spotter, actor };
        }
    }

    class PlayerHurt : Gameevent
    {
        public Player victim { get; set; }
        public int HP { get; set; }
        public int armor { get; set; }
        public int armor_damage { get; set; }
        public int HP_damage { get; set; }
        public int hitgroup { get; set; }
        public Weapon weapon { get; set; }

        public override Player[] getPlayers()
        {
            return new Player[] { actor, victim };
        }
    }

    class PlayerKilled : PlayerHurt
    {

        public bool headshot { get; set; }
        public int penetrated { get; set; }

        // buildLink() should be inherited by PlayerHurt

    }
}
