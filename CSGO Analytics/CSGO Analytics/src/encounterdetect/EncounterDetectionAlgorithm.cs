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
        public AlgorithmMode mode = AlgorithmMode.EUCLID_COMBATLINKS;

        //
        // VARIABLES AND CONSTANTS
        //

        // Timeouts in sec.
        private const float TAU = 20;
        private const float ENCOUNTER_TIMEOUT = 20;
        private const float WEAPONFIRE_VICTIMSEARCH_TIMEOUT = 4;
        private const float PLAYERHURT_WEAPONFIRESEARCH_TIMEOUT = 4;

        /// <summary>
        /// Tickrate of the demo this algorithm runs on. 
        /// </summary>
        public float tickrate;

        /// <summary>
        /// All players - communicated by the meta-data - which are participating in this match.
        /// </summary>
        private Player[] players;

        /// <summary>
        /// All data we have from this match.
        /// </summary>
        private Match match;


        /// <summary>
        /// Map for CSGO IDS to our own. CSGO is using different IDs for their entities every match.(Watch out for mysterious id changes while the match runs!!)
        /// </summary>
        private Dictionary<int, int> mappedPlayerIDs = new Dictionary<int, int>();

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
            this.match = gamestate.match;
            this.tickrate = gamestate.meta.tickrate;
            this.players = gamestate.meta.players.ToArray();

            int ownid = 0;
            foreach (var player in players) // Map all CS Entity IDs to our own
            {
                mappedPlayerIDs.Add(player.player_id, ownid);
                ownid++;
            }

            initTables(ownid); // Initalize tables for all players(should be 10 for csgo)

        }

        public Player[] getPlayers()
        {
            return players;
        }

        public List<Encounter> getEncounters()
        {
            return closed_encounters;
        }

        /// <summary>
        /// All currently running, not timed out, encounters
        /// </summary>
        private List<Encounter> open_encounters = new List<Encounter>();

        /// <summary>
        /// Timed out encounters
        /// </summary>
        private List<Encounter> closed_encounters = new List<Encounter>();




        int pCount = 0;
        int wfCount = 0;
        int wfeCount = 0;
        int mCount = 0;
        int nCount = 0;
        int uCount = 0;
        int iCount = 0;



        private List<Encounter> predecessors = new List<Encounter>();
        /// <summary>
        /// 
        /// </summary>
        public MatchReplay run()
        {

            MatchReplay replay = new MatchReplay(); // Problem: empty ticks were thrown away -> gaps in tick_id -> cant use it as index in arrays

            var watch = System.Diagnostics.Stopwatch.StartNew();

            foreach (var round in match.rounds)
            {
                foreach (var tick in round.ticks) // Read all ticks
                {
                    foreach (var p in tick.getUpdatedPlayers()) // Update tables
                    {
                        int id = 0;

                        try { id = getID(p.player_id); }
                        catch (ArgumentOutOfRangeException e) // Watch out for id changes through spectator or something else
                        {
                            handleChangedID(p);
                        }
  

                        updatePosition(id, p.position.getAsArray());
                        updateFacing(id, p.facing.getAsArray());
                        updateDistance(id);

                        //updateSpotted(id, p.isSpotted); // Spotted boolean extracted from CSGO Demo

                        // Check if one of the enemy players saw the current player -> he was spotted
                        foreach(var counterplayer in tick.getUpdatedPlayers().Where(player => player.getTeam() != p.getTeam()))
                        {
                            //bool isSpotted = checkLineOfSight();
                           // updateSpotted(id, isSpotted); // Spotted boolean extracted from CSGO Demo

                        }
                    }


                    CombatComponent component = buildComponent(tick);

                    replay.insertData(tick, component); // Save the tick with its component for later replaying. 

                    //
                    // Everything after here is just sorting components into encounters (use component.parent to identify to which encounter it belongs)
                    //
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

                    // Check encounter timeouts every tick
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
            }
           

            //We are done. -> move open encounters to closed encounters
            closed_encounters.AddRange(open_encounters);
            open_encounters.Clear();

            // Dump stats to console
            pCount = nCount + uCount + mCount;
            Console.WriteLine("Component Predecessors handled: " + pCount);
            Console.WriteLine("New Encounter occured: " + nCount);
            Console.WriteLine("Encounter Merges occured: " + mCount);
            Console.WriteLine("Encounter Updates occured: " + uCount);
            Console.WriteLine("\nWeaponfire-Events total: " + wfeCount);
            Console.WriteLine("Weaponfire-Event where victims were found: " + wfCount);
            Console.WriteLine("Weaponfire-Events inserted into existing components: " + iCount);
            Console.WriteLine("\nNades tested for Supportlinks: " + activeNades.Count);
            Console.WriteLine("\n\n  Encounters found: " + closed_encounters.Count);

            watch.Stop();
            var sec = watch.ElapsedMilliseconds / 1000.0f;

            Console.WriteLine("\n  Time to run Algorithm: " + sec + "sec. \n");
            //TODO: dispose everything else. tickstream etc!!
            //tickstream.Dispose();

            return replay;
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
            foreach (var e in open_encounters.Where(e => e.tick_id - comp.tick_id <= TAU))
            {
                bool registered = false;
                foreach (var c in e.cs) //Really iterate over components? -> yes because we need c.players
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
            var css = cs.OrderBy(x => x.tick_id).ToList();
            int encounter_tick_id = cs.OrderBy(x => x.tick_id).ElementAt(0).tick_id;
            var merged_encounter = new Encounter(encounter_tick_id, css);
            merged_encounter.cs.ForEach(comp => comp.parent = merged_encounter); // Set new parent encounter for all components
            return new Encounter(encounter_tick_id, css);
        }






        /// <summary>
        /// Queue of all hurtevents that where fired. Use these to search for a coressponding weaponfire event.
        /// Value is the tick_id as int where the event happend
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
                        wfeCount++;
                        WeaponFire wf = (WeaponFire)g;
                        var victim = searchVictimCandidate(wf, tick.tick_id);

                        if (victim == null) // No candidate found. Either wait for a playerhurt event or there was not nearly victim
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
                    case "flash_exploded":
                        FlashNade flash = (FlashNade)g;
                        type = ComponentType.SUPPORTLINK;
                        // Each flashed player as long as it is not a teammate of the actor is tested for sight at a teammember of the flasher ( hase he prevented sight on one of his teammates) 
                        foreach (var flashedEnemyplayer in flash.flashedplayers.Where(player => player.team != flash.actor.team)) // Every player not in the team of the flasher
                        {
                            foreach (var counterplayer in players.Where(counterplayer => counterplayer.team != flashedEnemyplayer.team && flash.actor != counterplayer)) // Every player not in the team of the flashed(and not the flasher)
                            {
                                //if(testSight(flashedEnemyplayer, counterplayer)){ // Test if a flashed player can see a counterplayer -> create supportlink from nade thrower to counterplayer
                                //Link flashsupportlink = new Link(flash.actor, counterplayer, ComponentType.SUPPORTLINK, Direction.DEFAULT);
                                //links.Add(flashsupportlink);
                                //}
                            }
                        }
                        continue;
                    case "firenade_exploded":
                    case "smoke_exploded":
                        NadeEvents timedNadeStart = (NadeEvents)g;
                        activeNades.Add(timedNadeStart);
                        continue;
                    case "smoke_ended":
                    case "firenade_ended":
                        NadeEvents timedNadeEnd = (NadeEvents)g;
                        activeNades.Remove(timedNadeEnd);
                        continue;
                    default:
                        continue;
                }

                // Test for supportlinks created by nades(except flashbang) as these cant be read from events directly
                List<Player> supportedPlayers = getSupportedPlayers();
                foreach (var supportreciever in supportedPlayers)
                {
                    Link link = new Link(g.actor, supportreciever, ComponentType.SUPPORTLINK, Direction.DEFAULT);
                    links.Add(link);
                }

                int actor_id = getID(g.actor.player_id);
                int reciever_id = getID(reciever.player_id);

                if (type == ComponentType.SUPPORTLINK) continue;
                if (distance_table[actor_id][reciever_id] < 5000)
                {
                    Link link = new Link(g.actor, reciever, type, Direction.DEFAULT);
                    links.Add(link);
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

        private List<Player> getSupportedPlayers()
        {
            var supportedPlayers = new List<Player>();
            //Test for Line of sight vs smoke collision
            foreach (var nadeevent in activeNades)
            {
                foreach (var player in players.Where(player => player.team != nadeevent.actor.team))
                {
                    /*if(checkNadeSphereCollision(nadeevent, player)) //If they saw into the smoke
                        foreach(var counterplayer in players.Where(counterplayer => counterplayer.team != player.team){
                            if(testSight(player, counterplayer)){// Test if player1 can see player 2
                                supportePlayers.Add(counterplayer);
                            }
                        }
                     */
                }
            }
            return supportedPlayers;
        }


        /// <summary>
        /// When a hurtevent is registered we want to test if some of our pending weaponfire events match this playerhurt event.
        /// If so we have to insert the link that arises into the right Combatcomponent.
        /// </summary>
        /// <param name="ph"></param>
        /// <param name="tick_id"></param>
        private void handleIncomingHurtEvent(PlayerHurt ph, int tick_id)
        {
            registeredHurtEvents.Add(ph, tick_id);

            for (int index = pendingWeaponFireEvents.Count - 1; index >= 0; index--) //TODO: with where statement?
            {
                var item = pendingWeaponFireEvents.ElementAt(index);
                var weaponfireevent = item.Key;
                var wftick_id = item.Value;

                int tick_dt = Math.Abs(wftick_id - tick_id);
                if (tick_dt * tickrate / 1000 > 4)
                {
                    //If more than 4 seconds are between a shoot and a hit -> event is irrelevant now and can be removed
                    pendingWeaponFireEvents.Remove(weaponfireevent);
                    continue;
                }

                if (ph.actor.Equals(weaponfireevent.actor)) // We found a weaponfire event that matches the new playerhurt event
                {
                    Link insertLink = new Link(weaponfireevent.actor, ph.victim, ComponentType.COMBATLINK, Direction.DEFAULT); //TODO: only 15 or* 41 links found...seems a bit small

                    foreach (var en in open_encounters) // Search the component this link has to be inserted 
                    {
                        bool inserted = false;
                        foreach (var comp in en.cs.Where(comp => comp.tick_id == wftick_id))
                        {
                            iCount++;
                            comp.links.Add(insertLink);
                            inserted = true;
                            break;
                        }
                        if (inserted == true) //This should be useless if components and their tick_ids are unique
                            break;
                    }
                    pendingWeaponFireEvents.Remove(weaponfireevent); // Delete the weaponfire event from the queue
                }

            }
        }

        private List<Player> candidates = new List<Player>();
        /// <summary>
        /// Searches the player that has most probable Hurt another player with the given weapon fire event
        /// This just takes weaponfire events into account which came after a playerhurt event of the weaponfire event actor.
        /// And in most cases a player fires and misses therefore theres a long time between when he might hit the seen opponent because he hides. But still he saw and shot at him. These events are lost here
        /// </summary>
        /// <param name="wf"></param>
        /// <returns></returns>
        private Player searchVictimCandidate(WeaponFire wf, int tick_id)
        {

            for (int index = registeredHurtEvents.Count - 1; index >= 0; index--)
            {
                var item = registeredHurtEvents.ElementAt(index);
                var hurtevent = item.Key;
                var htick_id = item.Value;

                int tick_dt = Math.Abs(htick_id - tick_id);
                if (tick_dt * tickrate / 1000 > 20) // 20 second timeout for hurt events
                {
                    registeredHurtEvents.Remove(hurtevent);
                    continue;
                }
                // Watch out for teamdamage. No wrong combatlinks !!
                if (wf.actor.Equals(hurtevent.actor) && hurtevent.victim.getTeam() != wf.actor.getTeam()) // If we find a actor that hurt somebody. this weaponfireevent is likely to be a part of his burst and is therefore a combatlink
                {
                    candidates.Add(hurtevent.victim);
                    registeredHurtEvents.Remove(hurtevent);
                    break;
                }
                else // We didnt find a matching hurtevent but there is still a chance for a later hurt event to suite for wf. so we store and try another time
                {
                    pendingWeaponFireEvents.Add(wf, tick_id);
                    break;

                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }
            if (candidates.Count == 1)
            {
                return candidates[0];
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
                    distance_table[entityid][i] = MathLibrary.getEuclidDistance3D(new Vector(position_table[entityid]), new Vector(position_table[i]));
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

        public int getID(int csid) // Problem with ID Mapping: Player disconnect or else changes ID of this player
        {
            int id;
            if (mappedPlayerIDs.TryGetValue(csid, out id))
            {
                return id;

            } else
            {
                Console.WriteLine("Could not map unkown csid: " + csid + ", on Analytics-ID. Maybe a random CS-ID change occured? -> Key needs update");
                throw new ArgumentOutOfRangeException(); //TODO: our own exception?
            }
        }

        /// <summary>
        /// IDs given from CS:GO can change after certain events -> this kills our table updates
        /// So we just add a new id for this player to the dictionary. getID is not injective!
        /// </summary>
        /// <param name="p"></param>
        private void handleChangedID(Player p)
        {
            int changedKey = -99; //Deprecated
            int value = -99;
            for (int i = 0; i < players.Count() - 1; i++)
            {
                if (players[i].playername.Equals(p.playername)) // Find the player in our initalisation array
                {
                    changedKey = players[i].player_id; // The old key we used but which is not up to date
                    value = i; // Our value is always the position in the initalisation playerarray
                    players[i].player_id = p.player_id; //update his old id to the new changed one but only here!
                }
            }
            mappedPlayerIDs.Add(p.player_id, value);

        }
    }
}
