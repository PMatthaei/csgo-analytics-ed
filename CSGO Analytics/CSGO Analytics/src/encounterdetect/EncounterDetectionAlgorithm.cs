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
        private float tau = 20;
        private float ENCOUNTER_TIMEOUT = 20;

        private float tickrate;

        public AlgorithmMode mode = AlgorithmMode.EUCLID_COMBATLINKS;

        /// <summary>
        /// All players participating in this match.
        /// </summary>
        private Player[] players;

        /// <summary>
        /// All ticks we have from this match.
        /// </summary>
        private List<Tick> ticks;


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



        public EncounterDetectionAlgorithm(Gamestate gamestate)
        {
            this.ticks = getTicks(gamestate);
            this.tickrate = gamestate.meta.tickrate;
            this.players = gamestate.meta.players.ToArray();

            int ownid = 0;
            foreach (var player in gamestate.meta.players) // Map all CS Entity IDs to our own
            {
                idMapping.Add(player.player_id, ownid);
                ownid++;
            }

            initTables(ownid); // Initalize tables for all players(should be 10 for csgo)

        }

        public Player[] getPlayers()
        {
            return players;
        }

        /// <summary>
        /// Returns a list of all ticks
        /// </summary>
        /// <param name="rounds"></param>
        /// <returns></returns>
        public List<Tick> getTicks(Gamestate gs)
        {
            List<Tick> ticks = new List<Tick>();

            foreach (var r in gs.match.rounds)
            {
                ticks.AddRange(r.ticks);
            }
            return ticks;
        }


        private List<Encounter> open_encounters = new List<Encounter>();
        private List<Encounter> closed_encounters = new List<Encounter>();
        private List<Encounter> predecessors = new List<Encounter>();


        int pCount = 0;
        int wfCount = 0;
        int mCount = 0;
        int nCount = 0;
        int uCount = 0;
        int iCount = 0;

        /// <summary>
        /// 
        /// </summary>
        public void run()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            foreach (var tick in ticks) // Read all ticks
            {
                //Console.WriteLine("Current tick: " + tick.tick_id);

                foreach (var p in tick.getUpdatedPlayers()) // Update tables
                {
                    updatePosition(getID(p.player_id), p.position.getAsArray());
                    //updateFacing(getID(p.player_id), p.position.getAsArray()); //TODO how handle facing and calculate field of view? Facing class!?!?
                    //updateSpotted(getID(p.player_id), p.spotted); //TODO where calc who spotted him?
                    updateDistance(getID(p.player_id));
                }


                CombatComponent component = buildComponent(tick);


                if (component == null) // No component in this tick
                    continue;

                predecessors = searchPredecessors(component); // Check if this component has predecessors

                if (predecessors.Count == 0)
                {
                    open_encounters.Add(new Encounter(component)); nCount++;

                }


                if (predecessors.Count == 1)
                {
                    predecessors.ElementAt(0).update(component); uCount++;

                }


                if (predecessors.Count > 1)
                {
                    // Remove all predecessor encounters from open encounters because we re-add them as joint_encounter
                    open_encounters.RemoveAll((Encounter e) => { return predecessors.Contains(e); }); //TODO: is contains working? -> if not encounters slip through and waste memory.... a lot
                    var joint_encounter = join(predecessors); // Merge encounters holding these predecessors
                    joint_encounter.update(component);
                    open_encounters.Add(joint_encounter);
                    mCount++;
                }

                predecessors.Clear();

                // Check encounter timeouts
                for (int i = open_encounters.Count - 1; i >= 0; i--)
                {
                    Encounter e = open_encounters[i];
                    if (Math.Abs(e.tick_id - tick.tick_id) > ENCOUNTER_TIMEOUT)
                    {
                        open_encounters.Remove(e);
                        closed_encounters.Add(e);
                    }
                }
                // NEXT TICK

            } //NO TICKS LEFT

            //We are done. -> move open encounters to closed encounters
            closed_encounters.AddRange(open_encounters);
            open_encounters.Clear();

            // Dump stats to console
            pCount = nCount + uCount + mCount;
            Console.WriteLine("Component Predecessors handled: " + pCount);
            Console.WriteLine("New Encounter occured: " + nCount);
            Console.WriteLine("Encounter Merges occured: " + mCount);
            Console.WriteLine("Encounter Updates occured: " + uCount);
            Console.WriteLine("Weaponfire-Event victims found: " + wfCount);
            Console.WriteLine("Weaponfire-Events inserted into existing components: " + iCount);
            Console.WriteLine("\n Encounters found: " + closed_encounters.Count);

            watch.Stop();
            var sec = watch.ElapsedMilliseconds / 1000.0f;

            Console.WriteLine("Time to run Algorithm: " + sec + "sec. \n");
            //TODO: dispose everything else. tickstream etc!!
            //tickstream.Dispose();
        }

        public List<Encounter> getEncounters()
        {
            return closed_encounters;
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
                bool registered = false;
                foreach (var c in e.cs)
                {
                    int dt = e.tick_id - comp.tick_id;
                    if (dt <= tau)
                    {
                        // Test if c and comp have at least two players from different teams in common -> Intersection of lists
                        var intersectPlayers = c.players.Intersect(comp.players).ToList();

                        if (intersectPlayers.Count < 2)
                            continue;

                        var knownteam = intersectPlayers[0].getTeam(); //TODO: kürzer
                        for (int i = 1; i < intersectPlayers.Count(); i++)
                        {
                            var p = intersectPlayers[i];
                            //Is team different to one we know.
                            //If so we have all criterias for a successor and this encounter e is a predecessor of the component comp
                            if (knownteam != Team.None && knownteam != p.getTeam()) 
                            {
                                predecessors.Add(e);
                                registered = true; // Stop multiple adding of e
                                break;

                            }
                        }

                        if (registered) break;

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
        private Encounter join(List<Encounter> predecessors) //TODO: Problem: high tau increases concated merged encounters and therefore memory. where is the problem?
        {
            List<CombatComponent> cs = new List<CombatComponent>();
            foreach (var e in predecessors)
            {
                cs.AddRange(e.cs); // Watch for OutOfMemory here if too many predecessors add up!! 
            }
            var css = cs.OrderBy(x => x.tick_id);
            int encounter_tick_id = cs.OrderBy(x => x.tick_id).ElementAt(0).tick_id;
            return new Encounter(encounter_tick_id, cs);
        }






        /// <summary>
        /// Queue of all hurtevents that where fired. Use these to search for a coressponding weaponfire event.
        /// </summary>
        private Dictionary<PlayerHurt, int> registeredHurtEvents = new Dictionary<PlayerHurt, int>();


        /// <summary>
        /// Weaponfire events that are waiting for their check.
        /// </summary>
        private Dictionary<WeaponFire, int> pendingWeaponFireEvents = new Dictionary<WeaponFire, int>();

        /// <summary>
        /// Active nades such as smoke and fire nades which have not ended.
        /// </summary>
        private List<NadeEvents> activeNades = new List<NadeEvents>();

        /// <summary>
        /// Feeds the component with a link resulting of the given gameevent.
        /// </summary>
        /// <param name="component"></param>
        /// <param name="g"></param>
        private CombatComponent buildComponent(Tick tick)
        {

            List<Link> links = new List<Link>();

            foreach (var g in tick.tickevents) // Read all gameevents in that tick and build a component with it
            {
                Player reciever = null;
                ComponentType type = 0;

                switch (g.gameevent) //TODO: SWITCH OR implement in event class? problem: switch is mess and classes need sources from gamestate
                {
                    //
                    // CSGO GAMEEVENTS
                    //

                    //
                    //  Combatlinks
                    //
                    case "player_hurt":
                        PlayerHurt ph = (PlayerHurt)g;
                        reciever = ph.victim;
                        type = ComponentType.COMBATLINK;

                        handleIncomingHurtEvent(ph, tick.tick_id);

                        break;
                    case "player_killed":
                        PlayerKilled pk = (PlayerKilled)g;
                        reciever = pk.victim;
                        type = ComponentType.COMBATLINK;

                        break;
                    case "weapon_fire":
                        WeaponFire wf = (WeaponFire)g;
                        var victim = searchVictimCandidate(wf, tick.tick_id);

                        if (victim == null)
                            continue;
                        wfCount++;
                        reciever = victim;
                        type = ComponentType.COMBATLINK;
                        break;
                    case "player_spotted":
                        /*PlayerSpotted ps = (PlayerSpotted)g;
                        type = ComponentType.COMBATLINK;*/

                        continue;

                    //    
                    //  Supportlinks
                    //
                    case "smoke_exploded":
                        NadeEvents smokestart = (NadeEvents)g;
                        reciever = null;
                        type = ComponentType.SUPPORTLINK;

                        activeNades.Add(smokestart);
                        continue;
                        break;
                    case "smoke_ended":
                        NadeEvents smokeend = (NadeEvents)g;
                        reciever = null;
                        type = ComponentType.SUPPORTLINK;

                        activeNades.Remove(smokeend); // Does this really get the right nade?
                        continue;
                        break;
                    default:
                        continue; //Cant build Link with this event
                }

                int actor_id = getID(g.actor.player_id);
                int reciever_id = getID(reciever.player_id);

                if (distance_table[actor_id][reciever_id] < 5000)
                {
                    Link link = new Link(g.actor, reciever, type, Direction.DEFAULT);
                    links.Add(link); //Add links
                }

            }

            CombatComponent combcomp = null;
            if (links.Count != 0)
            {
                combcomp = new CombatComponent(tick.tick_id);
                combcomp.links = links;
                combcomp.assignPlayers(); // Assign players in this encounter from all links
            }

            return combcomp;
        }

        private void handleIncomingHurtEvent(PlayerHurt ph, int tick_id)
        {
            registeredHurtEvents.Add(ph, tick_id);

            for (int index = pendingWeaponFireEvents.Count - 1; index >= 0; index--)
            {
                var item = pendingWeaponFireEvents.ElementAt(index);
                var weaponfireevent = item.Key;
                var wftick_id = item.Value;

                int tick_dt = Math.Abs(wftick_id - tick_id);
                if (tick_dt * tickrate / 1000 > 20)
                {
                    //If more than 20 seconds are between a shoot and a hit -> event is irrelevant now and can be removed
                    pendingWeaponFireEvents.Remove(weaponfireevent);
                    continue;
                }

                if (ph.actor.Equals(weaponfireevent.actor)) // We found a weaponfire event that matches the new playerhurt event
                {
                    //TODO: insert the link which will be created into the right component
                    iCount++;
                    pendingWeaponFireEvents.Remove(weaponfireevent); // Delete the weaponfire event from the queue
                }
                
            }
        }

        private List<Player> candidates = new List<Player>();
        /// <summary>
        /// Searches the player that has most probable Hurt another player with the given weapon fire event
        /// </summary>
        /// <param name="wf"></param>
        /// <returns></returns>
        private Player searchVictimCandidate(WeaponFire wf, int tick_id)
        {
            Console.WriteLine(registeredHurtEvents.Count);
            // This just takes weaponfire events into account which came after a playerhurt event of the weaponfire event actor
            // And in most cases a player fires and misses and theres a long time between he might hit the seen opponent because he hides. But still he saw and shot at him. these events are lost here
            for (int index = registeredHurtEvents.Count - 1; index >= 0; index--)
            {
                var item = registeredHurtEvents.ElementAt(index);
                var hurtevent = item.Key;
                var htick_id = item.Value;

                int tick_dt = Math.Abs(htick_id - tick_id);
                if (tick_dt * tickrate / 1000 > 20)
                {
                    //If more than 20 seconds are between a shoot and a hit. this hurtevent is irrelevant
                    registeredHurtEvents.Remove(hurtevent);
                    continue;
                }

                if (wf.actor.Equals(hurtevent.actor)) // If we find a actor that hurt somebody this weaponfireevent is likely to be a part of his burst and is therefore a combatlink
                {
                    candidates.Add(hurtevent.victim);
                    registeredHurtEvents.Remove(hurtevent);
                    break;
                } else // We didnt find a matching hurtevent but there is still a chance for a later hurt event to suite for wf. so we store and try another time
                {
                    pendingWeaponFireEvents.Add(wf, tick_id);
                    break;

                }
            }


            if (candidates.Count == 0)
            {
                return null;
            }
            //Console.WriteLine("Candidates found: " + candidates.Count);
            //Console.ReadLine();
            var victim = candidates[0]; //TODO: search probablest attacker!
            candidates.Clear();
            return victim; 
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
                return id;
            else
                Console.WriteLine("Can`t map csid: " + csid + ", on id"); return -99;
        }
    }
}
