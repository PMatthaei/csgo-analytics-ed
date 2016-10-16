using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.data.gameobjects;
using CSGO_Analytics.src.encounterdetect;

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
        private float[][] facing_table;


        public EncounterDetectionAlgorithm(TickStream tstream)
        {
            this.tickstream = tstream;
        }

        public void run()
        {
            initTables(10); //Initalize Table for 10 players

            while (tickstream.hasNextTick()) // Read all ticks  //TODO: make all unnecessary stuff disposable
            {
                var tick = tickstream.readTick();

                foreach (Player p in tick.getPlayerUpdates()) // Update tables
                {
                    updatePosition(p.getID(), p.getPosition().getAsArray());
                    updateFacing(p.getID(), p.getFacing().getAsArray()); //TODO: facing class? and how handle player id
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

                    for (int i = open_encounters.Count - 1; i >= 0; i--)
                    {
                        Encounter e = open_encounters[i];
                        if (e.hasTimeout())
                        {
                            open_encounters.Remove(e);
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
            switch (g.gtype)
            {
                case GameeventType.PLAYER_HURT:
                    break;
                case GameeventType.PLAYER_KILLED:
                    break;
                case GameeventType.PLAYER_JUMPED:
                    break;
                case GameeventType.PLAYER_STEPPED:
                    break;
                case GameeventType.PLAYER_POSITIONUPDATE:
                    break;
                case GameeventType.WEAPON_FIRE:
                    break;
                case GameeventType.GRENADE_START:
                    break;
                case GameeventType.GRENADE_STOP:
                    break;
                default:
                    break;
            }
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

            facing_table = new float[playeramount][];
            for (int i = 0; i < facing_table.Length; i++)
            {
                position_table[i] = new float[2]; // yaw , pitch
            }
        }


        private void updatePosition(int entityid, float[] newpos)
        {
            for (int i = 0; i < position_table[entityid].Length; i++)
            {
                position_table[entityid][i] = newpos[i];
            }
        }


        private void updateFacing(int entityid, float[] newpos)
        {
            for (int i = 0; i < facing_table[entityid].Length; i++)
            {
                facing_table[entityid][i] = newpos[i];
            }
        }
    }
}
