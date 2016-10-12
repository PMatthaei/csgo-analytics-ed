using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.data.gameobjects;

namespace CSGO_Analytics.src.encounterdetect
{

    class EncounterDetectionAlgorithm
    {
        //
        // VARIABLES AND CONSTANTS
        //

        /// <summary>
        /// Time till weapon fire event is neglected (in ms)
        /// </summary>
        private static float TTD_WEAPONFIRE = 30.0f;




        private TickStream tickstream;

        private List<Encounter> open_encounters = new List<Encounter>();
        private List<Encounter> closed_encounters = new List<Encounter>();

        private List<Encounter> predecessors = new List<Encounter>();

        /// <summary>
        /// Matrix to save player positions
        ///            x    y       z
        /// f.e. [[123.23, 65.6, -43.4],[], ...]
        /// </summary>
        private float[][] position_table;

        /// <summary>
        /// Matrix to save player aiming vectors
        ///         yaw    pitch    
        /// f.e. [[231.23, 12.23], [... , ...], ...]
        /// 
        /// </summary>
        private float[][] direction_table;




        public void run()
        {
            initTables(10); //Initalize Table for 10 players

            while (tickstream.hasNextTick()) // Read all ticks  //TODO: make all unnecessary stuff disposable
            {
                var tick = tickstream.readTick();

                foreach (Player p in tick.getPlayerUpdates()) // Update tables
                {
                    updatePosition(p.getID(), p.getPosition().getAsArray());
                    updateDirection(p.getID(), p.getFacing().getAsArray()); //TODO: facing class?
                }

                foreach (Gameevent g in tick.getGameEvents()) // Read all gameevents in that tick
                {
                    var component = buildComponent(g); //
                    predecessors = searchPredecessors(component);

                    if (predecessors.Count == 0)
                        continue;

                    if (predecessors.Count == 1)
                        predecessors.ElementAt(0).update(component);

                    if (predecessors.Count > 1)
                    {
                        open_encounters.RemoveAll((Encounter e) => { return predecessors.Contains(e); });
                        var joint_encounter = join(predecessors);
                        joint_encounter.update(component);
                        open_encounters.Add(joint_encounter);
                    }


                    foreach (Encounter e in open_encounters)
                    {
                        if (e.hasTimeout())
                        {
                            open_encounters.Remove(e); // TODO might be a problem without iterator!!
                            closed_encounters.Add(e);
                        }

                    }

                    predecessors.Clear();
                }
            } //NO TICKS LEFT


        }

        private List<Encounter> searchPredecessors(EncounterComponent comp)
        {
            return null;
        }

        private Encounter join(List<Encounter> predecessors)
        {
            return null;
        }

        private EncounterComponent buildComponent(Gameevent g)
        {
            return null;
        }






        //
        //
        // INITALIZATION AND UPDATE METHODS
        //
        //
        private void initTables(int playeramount)
        {
            position_table = new float[playeramount][];
            for (int i = 0; i < position_table.Length; i++)
            {
                position_table[i] = new float[3]; // x, y , z
            }

            direction_table = new float[playeramount][];
            for (int i = 0; i < direction_table.Length; i++)
            {
                position_table[i] = new float[2]; // yaw , pitch
            }
        }


        private void updateTables()
        {
        }


        private void updatePosition(int entityid, float[] newpos)
        {
            for (int i = 0; i < position_table[entityid].Length; i++)
            {
                position_table[entityid][i] = newpos[i];
            }
        }


        private void updateDirection(int entityid, float[] newpos)
        {
            for (int i = 0; i < direction_table[entityid].Length; i++)
            {
                direction_table[entityid][i] = newpos[i];
            }
        }
    }
}
