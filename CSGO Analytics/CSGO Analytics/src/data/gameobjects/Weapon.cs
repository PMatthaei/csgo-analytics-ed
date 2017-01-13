using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_Analytics.src.data.gameobjects
{
    public enum WeaponType
    {
        Unknown = 0,
        Gravity = 999,

        //Pistoles
        P2000 = 1,
        Glock = 2,
        P250 = 3,
        Deagle = 4,
        FiveSeven = 5,
        DualBarettas = 6,
        Tec9 = 7,
        CZ = 8,
        USP = 9,
        Revolver = 10,

        //SMGs
        MP7 = 101,
        MP9 = 102,
        Bizon = 103,
        Mac10 = 104,
        UMP = 105,
        P90 = 106,

        //Heavy
        SawedOff = 201,
        Nova = 202,
        Swag7 = 203,
        XM1014 = 204,
        M249 = 205,
        Negev = 206,

        //Rifle
        Gallil = 301,
        Famas = 302,
        AK47 = 303,
        M4A4 = 304,
        M4A1 = 305,
        Scout = 306,
        SG556 = 307,
        AUG = 308,
        AWP = 309,
        Scar20 = 310,
        G3SG1 = 311,

        //Equipment
        Zeus = 401,
        Kevlar = 402,
        Helmet = 403,
        Bomb = 404,
        Knife = 405,
        DefuseKit = 406,
        World = 407,

        //Grenades
        Decoy = 501,
        Molotov = 502,
        Incendiary = 503,
        Flash = 504,
        Smoke = 505,
        HE = 506
    }

    public enum WeaponCategorie
    {
        Unknown = 0,
        Pistol = 1,
        SMG = 2,
        Heavy = 3,
        Rifle = 4,
        Equipment = 5,
        Grenade = 6,
    }


    public class Weapon
    {


        /// <summary>
        /// Owner of this weapon
        /// </summary>
        //public Player owner { get; set; } // Is always null. Not correctly returned by DemoInfo?

        public string name { get; set; }

        /// <summary>
        /// Type of the weapon
        /// </summary>
        public WeaponType weaponType { get; set; }

        /// <summary>
        /// Categorie of the weapon
        /// </summary>
        public WeaponCategorie weaponCategorie { get; set; }

        /// <summary>
        /// Ammo left in the magazine. NOT! total ammo for this weapon
        /// </summary>
        public int ammo_in_magazine { get; set; }

        // Weapon data
        //private int currentAmmo;

        //private int maxAmmo;

        //private bool isSilenced;
    }
}
