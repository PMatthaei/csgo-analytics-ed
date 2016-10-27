using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.math;
using System.Windows.Shapes;

namespace CSGO_Analytics.src.data.gameobjects
{
    public enum Team { T, CT, None };

    public class Player : GameObject
    {
        // Player data
        public int health;

        public int armor;

        public bool hasHelmet;

        public bool hasDefuser;

        public bool hasBomb;

        public int attackrange;

        /// <summary>
        /// Team player belongs to
        /// </summary>
        public Team team;

        /// <summary>
        /// Current facing of the player //TODO
        /// </summary>
        public Vector facing;

        /// <summary>
        /// List of equipement - first weapon is primary
        /// </summary>
        public List<Weapon> weapons;


        public bool isDead()
        {
            if (health == 0)
                return true;
            else
                return false;
        }

        public Weapon getPrimaryWeapon()
        {
            if (weapons[0] == null)
                //errorlog
                return null;

            return weapons[0];
        }

        public Vector getFacing()
        {
            return facing;
        }

        public bool sameTeam(Player p)
        {
            if (team == p.team)
                return true;

            return false;
        }

        public override bool Equals(object obj)
        {
            var p = (Player)obj;
            if (entityid == p.entityid && name == p.name && team == p.team)
                return true;

            return false;
        }

        public override int GetHashCode()
        {
            return this.entityid.GetHashCode();
        }
    }
}
