using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_Analytics.src.data.gameobjects
{
    enum WeaponCategorie { NADE, RIFLE, PISTOL, KNIFE, SHOTGUN, MG, SMG, TAZER };

    enum WeaponType {
        HEGrenade,
        Flash,
        Smoke,
        Molotov,
        Decoy,
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

    public class Weapon
    {
        /// <summary>
        /// Type of the weapon to distinguish it later
        /// </summary>
        //private WeaponType weaponType;

        /// <summary>
        /// Owner of this weapon
        /// </summary>
        //public Player owner { get; set; } // Is always null. Not correctly returned by DemoInfo?

        public string name { get; set; }

        public int ammo_in_magazine { get; set; }

        // Weapon data
        //private int currentAmmo;

        //private int maxAmmo;

        //private bool isSilenced;
    }
}
