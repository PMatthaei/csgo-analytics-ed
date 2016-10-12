using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.math;

namespace CSGO_Analytics.src.data.gameobjects
{
    enum Team { T, CT };

    class Player : GameObject
    {
        // Player data
        private int health;

        private int armor;

        private bool hasHelmet;

        private bool hasDefuser;

        private bool hasBomb;


        /// <summary>
        /// Current facing of the player //TODO
        /// </summary>
        private Vector facing;

        /// <summary>
        /// List of equipement - first weapon is primary
        /// </summary>
        private List<Weapon> weapons;

        /// <summary>
        /// Team player belongs to
        /// </summary>
        private Team team;
        

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
    }
}
