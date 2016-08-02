using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGOStatsED.src.data.gameobjects
{
    enum WeaponType { NADE, RIFLE, PISTOL, KNIFE, SHOTGUN, MG , SMG, TAZER };

    class Weapon
    {
        public WeaponType type;
        public string name;
        public Player owner;
        public int last_dealt_damage;
        public int current_ammo;
    }
}
