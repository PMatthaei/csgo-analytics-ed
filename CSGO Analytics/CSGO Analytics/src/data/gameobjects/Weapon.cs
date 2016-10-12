using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_Analytics.src.data.gameobjects
{
    enum WeaponCategorie { NADE, RIFLE, PISTOL, KNIFE, SHOTGUN, MG, SMG, TAZER };

    enum WeaponType {
        AK_47,
        TEC_9,
        PP_BIZON,
        SSG_503,
        M4A4,
        M4A1_S,
        USPS,
        GLOCK,
        AWP,
        SG,
        UMP,
        MP7,
        MP9,
        NOVA,

    };

    class Weapon : GameObject
    {
        /// <summary>
        /// Type of the weapon to distinguish it later
        /// </summary>
        private WeaponType weaponType;

        /// <summary>
        /// 
        /// </summary>
        private Player owner;

        /// <summary>
        /// Ammo left in magazine
        /// </summary>
        private int currentAmmo;

        /// <summary>
        /// Ammo magazine has after reloading 
        /// </summary>
        private int maxAmmo;


        private bool isSilenced;
    }
}
