using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.data.gameobjects;
using CSGO_Analytics.src.encounterdetect;
using CSGO_Analytics.src.math;
using CSGO_Analytics.src.json.jsonobjects;
using CSGO_Analytics.src.data.gameevents;

namespace CSGO_Analytics.src.encounterdetect
{
    public enum AlgorithmMode
    {
        EUCLID_COMBATLINKS,
        FOV_COMBATLINKS,
        SIGHT_COMBATLINKS,
    };

    public class EncounterDetectionAlgorithm
    {
        //
        // VARIABLES AND CONSTANTS
        //
        private float tau = 0.5f;

        public AlgorithmMode mode = AlgorithmMode.EUCLID_COMBATLINKS;

        private List<Tick> ticks;

        private List<Encounter> open_encounters = new List<Encounter>();
        private List<Encounter> closed_encounters = new List<Encounter>();
        private List<Encounter> predecessors = new List<Encounter>();


        /// <summary>
        /// Map for CSGO IDS to our own. CSGO is using different IDs for their entities every match.
        /// </summary>
        private Dictionary<int, int> idMapping = new Dictionary<int, int>();

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



        public EncounterDetectionAlgorithm(JSONGamestate gamestate)
        {
            this.ticks = getTicks(gamestate);

            int ownid = 0;
            foreach (var player in gamestate.meta.players) // Map all CS Entity IDs to our own
            {
                idMapping.Add(player.player_id, ownid);
                ownid++;
            }

            initTables(ownid); // Initalize tables for all players(should be 10 for csgo)

        }

        /// <summary>
        /// Returns a list of all ticks
        /// </summary>
        /// <param name="rounds"></param>
        /// <returns></returns>
        public List<Tick> getTicks(JSONGamestate gs)
        {
            List<Tick> ticks = new List<Tick>();

            foreach (var r in gs.match.rounds)
            {
                ticks.AddRange(r.ticks);
            }
            return ticks;
        }






        /// <summary>
        /// 
        /// </summary>
        public void run()
        {

            foreach (var tick in ticks) // Read all ticks
            {
                Console.WriteLine("Current tick: " + tick.tick_id);

                foreach (var p in tick.getUpdatedPlayers()) // Update tables
                {
                    updatePosition(getID(p.player_id), p.position.getAsArray());
                    updateFacing(getID(p.player_id), p.position.getAsArray());
                    //updateSpotted(getID(p.player_id), p.spotted); //TODO
                    updateDistance(getID(p.player_id)); //TODO:   
                }


                CombatComponent component = null;
                //CombatComponent component = buildComponent(tick);


                if (component == null) // No component in this tick
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

                // Check timeouts
                for (int i = open_encounters.Count - 1; i >= 0; i--)
                {
                    Encounter e = open_encounters[i];
                    if (e.hasTimeout(tick.tick_id))
                    {
                        open_encounters.Remove(e);
                        closed_encounters.Add(e);
                    }
                }
            } //NO TICKS LEFT

            //We are done. -> move open encounters to closed encounters
            closed_encounters.AddRange(open_encounters);
            open_encounters.Clear();

            //TODO: dispose everything else. tickstream etc!!
            //tickstream.Dispose();
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
                        var intersectPlayers = c.players.Intersect(comp.players);
                        if (intersectPlayers.Count() < 2)
                            continue;

                        var oldteam = Team.None; //TODO: kürzer
                        foreach (var p in intersectPlayers)
                        {
                            if (oldteam != Team.None && oldteam != p.getTeam()) //Is team different to one we know
                            {
                                predecessors.Add(e);
                                break; // We found a second team in the intersected players
                            }
                            else
                            {
                                oldteam = p.getTeam();
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


        private List<Link> links = new List<Link>();

        /// <summary>
        /// Feeds the component with a link resulting of the given gameevent.
        /// </summary>
        /// <param name="component"></param>
        /// <param name="g"></param>
        private CombatComponent buildComponent(Tick tick)
        {
            Link link = null;

            foreach (var g in tick.tickevents) // Read all gameevents in that tick and build a component with it
            {

                switch (g.gameevent) //TODO: SWITCH OR implement in event class? problem: switch is mess and classes need sources from gamestate
                {
                    //CSGO GAMEEVENTS
                    case "player_hurt":
                        PlayerHurt p = (PlayerHurt)g;
                        link = new Link(p.actor, p.victim, ComponentType.COMBATLINK, Direction.DEFAULT);
                        break;
                    case "player_killed":
                        PlayerKilled pk = (PlayerKilled)g;
                        link = new Link(pk.actor, pk.victim, ComponentType.COMBATLINK, Direction.DEFAULT);
                        break;
                    case "weapon_fire":
                        WeaponFire wf = (WeaponFire)g;
                        Player victim = null; //TODO: suche den anvisierten spieler
                        link = new Link(wf.actor, victim, ComponentType.COMBATLINK, Direction.DEFAULT);
                        break;
                    case "player_spotted":
                        PlayerSpotted sp = (PlayerSpotted)g;
                        sp.actor = null; //TODO: suche den gesehenen spieler
                        link = new Link(sp.spotter, sp.actor, ComponentType.COMBATLINK, Direction.DEFAULT);
                        break;
                    default:
                        continue; //Cant build Link with this event
                }

                if (link != null)
                {
                    links.Add(link); //Add links
                    link = null;
                }

            }

            CombatComponent combcomp = null;
            if (links.Count != 0)
            {
                combcomp = new CombatComponent(tick.tick_id);
                combcomp.links = links;
                links.Clear();
                combcomp.assignPlayers(); // fetch players in this encounter from all links
            }

            return combcomp;
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


        private void updateDistance(int entityid)
        {
            for (int i = 0; i < distance_table[entityid].Length; i++)
            {
                if (entityid != i)
                    distance_table[entityid][i] = MathUtils.getEuclidDistance3D(new Vector(position_table[entityid]), new Vector(position_table[i]));
            }
        }

        private void updateSpotted(int entityid, bool spotted)
        {
            spotted_table[entityid][entityid] = spotted;
            // TODO: Check who spotted the player entityid?
        }


        //
        //
        // HELPING METHODS
        //
        //

        public int getID(int csid)
        {
            int id;
            if (idMapping.TryGetValue(csid, out id))
            {
                return id;
            }
            else
            {
                Console.WriteLine("Can`t map id: " + csid);
                return -99;
            }
        }
    }
}
