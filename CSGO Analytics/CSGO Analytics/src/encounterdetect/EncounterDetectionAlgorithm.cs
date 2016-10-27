﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.data.gameobjects;
using CSGO_Analytics.src.encounterdetect;
using CSGO_Analytics.src.math;

namespace CSGO_Analytics.src.encounterdetect
{
    enum AlgorithmMode
    {
        EUCLID_COMBATLINKS,
        FOV_COMBATLINKS,
        SIGHT_COMBATLINKS,
    };

    class EncounterDetectionAlgorithm
    {
        //
        // VARIABLES AND CONSTANTS
        //
        private float tau = 0.5f;

        public AlgorithmMode mode = AlgorithmMode.EUCLID_COMBATLINKS;



        private TickStream tickstream;

        private List<Encounter> open_encounters = new List<Encounter>();
        private List<Encounter> closed_encounters = new List<Encounter>();

        private List<Encounter> predecessors = new List<Encounter>();


        private Player[] players;

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

        /// <summary>
        /// Matrix to save player distance between each other
        /// </summary>
        private float[][] distance_table;

        /// <summary>
        /// Matrix to save player visibility between each other
        /// </summary>
        private bool[][] spotted_table;

        public EncounterDetectionAlgorithm(TickStream tstream)
        {
            this.tickstream = tstream;
            players = new Player[10];
        }

        public void run()
        {
            initTables(10); //Initalize tables for 10 players

            while (tickstream.hasNextTick()) // Read all ticks
            {
                using (var tick = tickstream.readTick())
                {

                    foreach (var p in tick.getUpdatedPlayers()) // Update tables
                    {
                        updatePosition(p.getID(), p.getPosition().getAsArray()); //Evtl anders als mit matrizen machen
                        updateFacing(p.getID(), p.getFacing().getAsArray()); //TODO: facing class? and how handle player id -> ids sind unterschiedlich von game zu game und nicht von 0-9
                        updateSpotted(); //TODO

                        updateDistances(); //TODO: 

                    }

                    CombatComponent component = null;

                    foreach (var g in tick.getGameEvents()) // Read all gameevents in that tick and build a component with it
                    {
                        buildComponent(component, g); //maybe call this feedComponent() (with gameevent links)
                    }

                    if (!(component is CombatComponent)) //TODO: makes no sense? see schubert code
                        continue;

                    predecessors = searchPredecessors(component); // Check if this component has predecessors

                    if (predecessors.Count == 0)
                        open_encounters.Add(new Encounter(component));

                    if (predecessors.Count == 1)
                        predecessors.ElementAt(0).update(component);

                    if (predecessors.Count > 1)
                    {
                        // Remove all predecessor encounters from open encounters because we re-add them as joint_encounter
                        open_encounters.RemoveAll((Encounter e) => { return predecessors.Contains(e); });
                        var joint_encounter = join(predecessors); // Merge encounters holding these predecessors
                        joint_encounter.update(component);
                        open_encounters.Add(joint_encounter);
                    }
                    predecessors.Clear();


                    for (int i = open_encounters.Count - 1; i >= 0; i--)
                    {
                        Encounter e = open_encounters[i];
                        if (e.hasTimeout(tick.tick_id))
                        {
                            open_encounters.Remove(e);
                            closed_encounters.Add(e);
                        }
                    }

                }
            } //NO TICKS LEFT

            //We are done. -> move open encounters to closed encounters
            closed_encounters.AddRange(open_encounters);
            open_encounters.Clear();

            //TODO: dispose everything else. tickstream etc!!
            tickstream.Dispose();
        }

        /// <summary>
        /// Searches all predecessor encounters of an component. or in other words:
        /// tests if a component is a successor of another encounters component
        /// </summary>
        /// <param name="comp"></param>
        /// <returns></returns>
        private List<Encounter> searchPredecessors(CombatComponent comp)
        {
            List<Encounter> predecessors = new List<Encounter>();
            foreach (var e in open_encounters)
            {
                foreach (var c in e.cs)
                {
                    int testtick_id = c.tick_id; //Maybe use only tickid of newest comp?
                    int dt = Math.Abs(testtick_id - comp.tick_id);

                    if (testtick_id + dt <= tau)
                    {
                        // -> Test if there are same players in component and predecessor and if there are at least two with different teams.
                        // Intersection of player(Vereinigung)
                        var doubleplayers = c.players.Intersect(comp.players);
                        if (doubleplayers.Count() < 2)
                            continue;

                        var oldteam = Team.None; //TODO: kürzer
                        foreach (var p in doubleplayers)
                        {
                            if (oldteam != Team.None && oldteam != p.team) //Is team different to one we know
                            {
                                predecessors.Add(e);
                                break; // We found a second team in the predecessorplayers
                            }
                            else
                            {
                                oldteam = p.team;
                            }
                        }

                    }

                }
            }

            return predecessors;
        }

        /// <summary>
        /// Joins a list of encounters into one single encounter (Merge-case).
        /// </summary>
        /// <param name="predecessors"></param>
        /// <returns></returns>
        private Encounter join(List<Encounter> predecessors)
        {
            List<CombatComponent> cs = new List<CombatComponent>();
            foreach (var e in predecessors)
            {
                cs.AddRange(e.cs);
            }
            int encounter_tick_id = cs.OrderBy(x => x.tick_id).ElementAt(0).tick_id;
            return new Encounter(encounter_tick_id, cs);
        }

        /// <summary>
        /// Feeds the component with a link resulting of the given gameevent.
        /// </summary>
        /// <param name="component"></param>
        /// <param name="g"></param>
        private void buildComponent(CombatComponent component, GameEvent g)
        {
            switch (mode)
            {
                case AlgorithmMode.EUCLID_COMBATLINKS:
                    float[] playerpos = position_table[g.actor.entityid];
                    for (int i = 0; i < position_table.Length; i++)
                    {
                        if (i == g.actor.entityid)
                            continue;

                        var arr = position_table[i];
                        var euclid_distance = MathUtils.getEuclidDistance3D(new Vector(playerpos), new Vector(arr));
                        if (g.actor.attackrange >= euclid_distance)
                        {
                            //new Link();
                        }
                    }
                    break;
                case AlgorithmMode.SIGHT_COMBATLINKS:
                    break;
                case AlgorithmMode.FOV_COMBATLINKS:
                    break;
            }
        }






        //
        //
        // INITALIZATION AND UPDATE METHODS FOR TABLES
        //
        //
        private void initTables(int playeramount)
        {
            position_table = new float[playeramount][];
            for (int i = 0; i < position_table.Length; i++)
            {
                position_table[i] = new float[3]; // x, y, z
            }

            facing_table = new float[playeramount][];
            for (int i = 0; i < facing_table.Length; i++)
            {
                facing_table[i] = new float[2]; // yaw , pitch
            }

            distance_table = new float[playeramount][];
            for (int i = 0; i < distance_table.Length; i++)
            {
                distance_table[i] = new float[playeramount]; // distance between each player
            }

            spotted_table = new bool[playeramount][];
            for (int i = 0; i < spotted_table.Length; i++)
            {
                spotted_table[i] = new bool[playeramount]; // spotted(true/false) between each player
            }
        }


        private void updatePosition(int entityid, float[] newpos)
        {
            for (int i = 0; i < position_table[entityid].Length; i++)
            {
                position_table[entityid][i] = newpos[i];
            }
        }


        private void updateFacing(int entityid, float[] newface)
        {
            for (int i = 0; i < facing_table[entityid].Length; i++)
            {
                facing_table[entityid][i] = newface[i];
            }
        }


        private void updateDistances() //TODO
        {
        }

        private void updateSpotted()//TODO
        {

        }
    }
}
