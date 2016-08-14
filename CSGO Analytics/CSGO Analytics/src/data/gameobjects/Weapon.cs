using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_Analytics.src.data.gameobjects
{
    enum WeaponType { NADE, RIFLE, PISTOL, KNIFE, SHOTGUN, MG , SMG, TAZER };

    class Weapon : GameObject
    {
        private WeaponType type;
        private int current_ammo;
        private int max_ammo;
    }
}
