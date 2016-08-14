using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_Analytics.src.encounterdetect
{

    class EncounterDetection
    {
        //Variables for Encounter Detection Algorithm
        /// <summary>
        /// Time till weapon fire event is neglected (in ms)
        /// </summary>
        private static float TTD_WEAPONFIRE = 30.0f;

        private Stream stream;

        private List<Encounter> open_encounters;
        private List<Encounter> closed_encounters;

        /// <summary>
        /// Matrix to save player positions
        /// </summary>
        private float[][] position_table;

        /// <summary>
        /// Matrix to save player aiming vectors
        /// </summary>
        private float[][] direction_table;

        public void buildLink()
        {

        }

        public void initTables(int playeramount)
        {
            position_table = new float[playeramount][];
            for (int i = 0; i < position_table.Length; i++)
            {
                position_table[i] = new float[3]; // x,y,z
            }

            direction_table = new float[playeramount][];
            for (int i = 0; i < direction_table.Length; i++)
            {
                position_table[i] = new float[2]; // yaw,pitch
            }
        }


        public void updateTables()
        {
        }


        public void updatePosition(int entityid)
        {
            for (int i = 0; i < position_table[entityid].Length; i++)
            {
                position_table[entityid][i] = 3.0f; // yaw,pitch
            }
        }


        public void updateDirection(int entityid)
        {
            for (int i = 0; i < direction_table[entityid].Length; i++)
            {
                direction_table[entityid][i] = 3.0f; // yaw,pitch
            }
        }
    }
}
