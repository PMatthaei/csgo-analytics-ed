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

        private const float ATTACKRANGE = 500.0f;
        private const float SUPPORTRANGE = 300.0f;

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
        int sfCount = 0;
        int sCount = 0;
        int wfCount = 0;
        int wfeCount = 0;
        int mCount = 0;
        int nCount = 0;
        int uCount = 0;
        int iCount = 0;

        int usCount = 0;
        int ussCount = 0;
        int stCount = 0;
        int dsCount = 0;

        int smokeAssistCount = 0;
        int fCount = 0;

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
                    foreach (var updatedPlayer in tick.getUpdatedPlayers()) // Update tables
                    {
                        int id = 0;

                        try { id = getID(updatedPlayer.player_id); }
                        catch (ArgumentOutOfRangeException e) // Watch out for id changes through spectator or something else
                        {
                            Console.WriteLine("Handling ID Out of Range: \n" + e.StackTrace);
                            handleChangedID(updatedPlayer);
                        }


                        updatePosition(id, updatedPlayer.position.getAsArray());
                        updateFacing(id, updatedPlayer.facing.getAsArray());
                        updateDistance(id);

                        //updateSpotted(id, p.isSpotted); // Spotted boolean extracted from CSGO Demo
                        if (updatedPlayer.isSpotted) stCount++;
                        // Check if the updatedPlayer sees an enemy or is seen by someone who was also updated
                        foreach (var counterplayer in tick.getUpdatedPlayers().Where(player => player.getTeam() != updatedPlayer.getTeam()))
                        {
                            //TODO: do these checks with tables and not just with updated players
                            bool updatedPlayerSpots = MathLibrary.isInFOV(updatedPlayer.position, updatedPlayer.facing.yaw, counterplayer.position); //Has the updated player spotted someone
                            bool updatedPlayerSpotted = MathLibrary.isInFOV(counterplayer.position, counterplayer.facing.yaw, updatedPlayer.position); //Has someone spotted the player
                            //updateSpotted(id, isSpotted);

                            if (updatedPlayerSpots) usCount++;
                            if (updatedPlayerSpotted) ussCount++;
                            if (updatedPlayerSpotted != updatedPlayerSpots) dsCount++;
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
            watch.Stop();
            var sec = watch.ElapsedMilliseconds / 1000.0f;

            // Clear data
            this.match = null;

            //We are done. -> move open encounters to closed encounters
            closed_encounters.AddRange(open_encounters);
            open_encounters.Clear();

            // Dump stats to console
            pCount = nCount + uCount + mCount;
            Console.WriteLine("Component Predecessors handled: " + pCount);
            Console.WriteLine("New Encounters occured: " + nCount);
            Console.WriteLine("Encounter Merges occured: " + mCount);
            Console.WriteLine("Encounter Updates occured: " + uCount);

            Console.WriteLine("\nWeaponfire-Events total: " + wfeCount);
            Console.WriteLine("Weaponfire-Event where victims were found: " + wfCount);
            Console.WriteLine("Weaponfire-Events inserted into existing components: " + iCount);


            Console.WriteLine("\nSpotted-Events occured: " + sCount);
            Console.WriteLine("Spotters found: " + sfCount);
            Console.WriteLine("\nPlayer is spotted in: " + stCount + " ticks(compare with UpdatedPlayer-Entries)");
            Console.WriteLine("Spotted-Events found by Algorithm with Spotrange 500: ");
            Console.WriteLine("UpdatedPlayer spotted someone: " + usCount);
            Console.WriteLine("UpdatedPlayer was spotted: " + ussCount);
            Console.WriteLine("Spotdifferences(only one can see the other): " + ussCount); //TODO: nie im selben tick gegenseitig spotten

            Console.WriteLine("\nNades tested for Supportlinks: " + activeNades.Count);
            Console.WriteLine("Smoke Supportlinks: " + smokeAssistCount);
            Console.WriteLine("Flash Supportlinks: " + fCount);


            Console.WriteLine("\n\n  Encounters found: " + closed_encounters.Count);

            Console.WriteLine("\n  Time to run Algorithm: " + sec + "sec. \n");

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
                        // Team different to one we know -> this encounter e is a predecessor of the component comp
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

            // TODO: Dead players are still present in data and build links
            //searchDistancebasedCombatLinks(tick, links);

            searchEventbasedCombatLinks(tick, links);

            // Test for supportlinks created by nades(except flashbang) as these cant be read from events directly
            links.AddRange(searchSupportlinks());

            CombatComponent combcomp = null;
            if (links.Count != 0)
            {
                combcomp = new CombatComponent(tick.tick_id);
                combcomp.links = links;
                combcomp.assignPlayers();
            }

            return combcomp;
        }






        /// <summary>
        /// Algorithm searching links simply on euclid distance
        /// </summary>
        /// <param name="tick"></param>
        /// <param name="links"></param>
        private void searchDistancebasedCombatLinks(Tick tick, List<Link> links)
        {
            foreach (var player in tick.getUpdatedPlayers())
            {
                for (int i = 0; i < distance_table[getID(player.player_id)].Length; i++)
                {
                    var actor = players[getID(player.player_id)];
                    var reciever = players[i];

                    if (distance_table[getID(player.player_id)][i] < ATTACKRANGE)
                    {
                        if (actor.getTeam() != reciever.getTeam())
                            links.Add(new Link(actor, reciever, LinkType.COMBATLINK, Direction.DEFAULT));
                    } else if(distance_table[getID(player.player_id)][i] < SUPPORTRANGE)
                    {
                        if (actor.getTeam() != reciever.getTeam())
                            links.Add(new Link(actor, reciever, LinkType.SUPPORTLINK, Direction.DEFAULT));
                    }
                }
            }
        }



        /// <summary>
        /// Algorithm searching links based on CS:GO Replay Events
        /// </summary>
        /// <param name="tick"></param>
        /// <param name="links"></param>
        private void searchEventbasedCombatLinks(Tick tick, List<Link> links)
        {
            foreach (var g in tick.tickevents) { // Read all gameevents in that tick and build links of them for the component
                switch (g.gameevent)
                {
                    //
                    //  Combatlinks
                    //
                    case "player_hurt":
                        PlayerHurt ph = (PlayerHurt)g;
                        links.Add(new Link(ph.actor, ph.victim, LinkType.COMBATLINK, Direction.DEFAULT));

                        handleIncomingHurtEvent(ph, tick.tick_id);

                        break;
                    case "player_killed":
                        PlayerKilled pk = (PlayerKilled)g;
                        links.Add(new Link(pk.actor, pk.victim, LinkType.COMBATLINK, Direction.DEFAULT));

                        break;
                    case "weapon_fire":
                        wfeCount++;
                        WeaponFire wf = (WeaponFire)g;
                        var potential_victim = searchVictimCandidate(wf, tick.tick_id);

                        // No candidate found. Either wait for a incoming playerhurt event or there was not nearly victim
                        if (potential_victim == null) 
                            break;
                        wfCount++;
                        links.Add(new Link(wf.actor, potential_victim, LinkType.COMBATLINK, Direction.DEFAULT));

                        break;
                    case "player_spotted":
                        PlayerSpotted ps = (PlayerSpotted)g;
                        var potential_spotted = searchSpottedCandidates(ps.actor);
                        sCount++;
                        if (potential_spotted == null) // This should not happend as these events are most likely to be true
                            break;
                        links.Add(new Link(ps.actor, potential_spotted, LinkType.COMBATLINK, Direction.DEFAULT));
                        sfCount++;
                        break;

                    //    
                    //  Supportlinks TODO: move this to somewhere else maybe getSupportlinks
                    //
                    case "flash_exploded":
                        FlashNade flash = (FlashNade)g;
                        // Each flashed player as long as it is not a teammate of the actor is tested for sight at a teammember of the flasher ( has he prevented sight on one of his teammates) 
                        if (flash.flashedplayers.Count == 0) continue;
                        foreach (var flashedEnemyplayer in flash.flashedplayers.Where(player => player.getTeam() != flash.actor.getTeam())) // Every player not in the team of the flasher(sort out all teamflashes)
                        {
                            if (flashedEnemyplayer.playername == "GOTV") continue; //TODO: prevent GOTV player from beginning tracked in the parser

                            var flashedenemyplayer_id = getID(flashedEnemyplayer.player_id);
                            var flashedpos = new Vector(position_table[flashedenemyplayer_id]);

                            links.Add(new Link(flash.actor, flashedEnemyplayer, LinkType.COMBATLINK, Direction.DEFAULT));
                            foreach (var teammate in players.Where(teamate => teamate.getTeam() == flash.actor.getTeam() && flash.actor != teamate))
                            {
                                var teammate_id = getID(teammate.player_id);
                                var teammatepos = new Vector(position_table[teammate_id]);
                                // Test if a flashed player can see a counterplayer -> create supportlink from nade thrower to counterplayer
                                if (MathLibrary.isInFOV(flashedpos, facing_table[flashedenemyplayer_id][0], teammatepos))
                                { 
                                    Link flashsupportlink = new Link(flash.actor, teammate, LinkType.SUPPORTLINK, Direction.DEFAULT);
                                    links.Add(flashsupportlink);
                                    fCount++;
                                }
                            }
                        }
                        break;
                    case "firenade_exploded":
                    case "smoke_exploded":
                        NadeEvents timedNadeStart = (NadeEvents)g;
                        activeNades.Add(timedNadeStart);
                        break;
                    case "smoke_ended":
                    case "firenade_ended":
                        NadeEvents timedNadeEnd = (NadeEvents)g;
                        activeNades.Remove(timedNadeEnd);
                        break;
                    default:
                        break;
                }

            }
        }

        private List<Link> searchSupportlinks()
        {
            var supportlinks = new List<Link>();

            // Find Supportlinks produced by smoke nades
            var smokenades = activeNades.Where(nade => nade.gameevent == "smoke_exploded");
            foreach (var nadeevent in smokenades)
            {
                foreach (var counterplayer in players.Where(player => player.getTeam() != nadeevent.actor.getTeam())) //TODO: cant work with players because its postion and yaw etc does not get updated
                {
                    var counterplayer_id = getID(counterplayer.player_id);
                    var counterplayerpos = new Vector(position_table[counterplayer_id]);
                    var counterplayerYaw = facing_table[counterplayer_id][0];

                    //If a player from the opposing team of the smoke thrower saw into the smoke
                    if (MathLibrary.vectorClipsSphere2D(nadeevent.position.x, nadeevent.position.y, 20, counterplayerpos, counterplayerYaw))
                    {
                        // Check if he could have seen a player from the thrower team
                        foreach (var teammate in players.Where(teammate => teammate.getTeam() == nadeevent.actor.getTeam()))
                        {
                            var teammate_id = getID(teammate.player_id);
                            var teammatepos = new Vector(position_table[teammate_id]);
                            // Test if the player who looked in the smoke can see a player from the oppposing( thrower) team
                            if (MathLibrary.isInFOV(counterplayerpos, counterplayerYaw, teammatepos))
                            {
                                // The actor supported a teammate -> Supportlink
                                Link link = new Link(nadeevent.actor, teammate, LinkType.SUPPORTLINK, Direction.DEFAULT);
                                supportlinks.Add(link);
                                smokeAssistCount++;
                            }
                        }
                    }
                }
            }

            // Test covered areas TODO:
            // Test weapon drop assists TODO:
            // Test kill assists TODO:
            return supportlinks;
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
                    Link insertLink = new Link(weaponfireevent.actor, ph.victim, LinkType.COMBATLINK, Direction.DEFAULT); //TODO: only 15 or* 41 links found...seems a bit small

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
                var player = candidates[0];
                candidates.Clear();
                return player;
            }
            //Console.WriteLine("Candidates found: " + candidates.Count);
            //Console.ReadLine();
            var victim = candidates[0]; //TODO: search probablest attacker!
            candidates.Clear();
            return victim;
        }



        /// <summary>
        /// Searches players who have spotted a certain player
        /// </summary>
        /// <param name="actor"></param>
        /// <returns></returns>
        private Player searchSpottedCandidates(Player actor)
        {
            var actor_id = getID(actor.player_id);
            var actorpos = new Vector(position_table[actor_id]);

            foreach (var counterplayer in players.Where(counterplayer => counterplayer.getTeam() != actor.getTeam()))
            {
                var counterplayer_id = getID(counterplayer.player_id);
                var counterplayerpos = new Vector(position_table[counterplayer_id]);
                var counterplayerYaw = facing_table[counterplayer_id][0];

                // Test if an enemy can see our actor
                if (MathLibrary.isInFOV(counterplayerpos, counterplayerYaw, actorpos))
                {
                    candidates.Add(counterplayer);
                }
            }

            if (candidates.Count == 0)
                return null;

            if (candidates.Count == 1)
            {
                var player = candidates[0];
                candidates.Clear();
                return player;
            }
            if (candidates.Count > 1) // The one with the shortest distance to the actor is the spotter (maybe search better condition)
            {
                var nearestplayer = candidates.OrderBy(candidate => MathLibrary.getEuclidDistance2D(candidate.position, actor.position)).ToList()[0];
                candidates.Clear();
                return nearestplayer;
            }
            return null;
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

            spotted_table = new bool[playeramount / 2 + 1][]; // +1 because we need one extra row to determine who spotted who
            for (int i = 0; i < spotted_table.Length; i++)
            {
                spotted_table[i] = new bool[playeramount / 2 + 1]; // spotted(true/false) between each player
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
                    distance_table[entityid][i] = (float)MathLibrary.getEuclidDistance2D(new Vector(position_table[entityid]), new Vector(position_table[i]));
            }
        }

        private void updateSpotted(int acid, int recid, bool spotted, bool actor, bool reciever)
        {
            spotted_table[acid][recid] = spotted;
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
                return id;
            else
                Console.WriteLine("Could not map unkown csid: " + csid + ", on Analytics-ID. Maybe a random CS-ID change occured? -> Key needs update");
                foreach(var pair in mappedPlayerIDs.Keys)
                {
                Console.WriteLine("Key mapped: "+ pair);
                }
            throw new ArgumentOutOfRangeException(); //TODO: our own exception?

        }

        /// <summary>
        /// IDs given from CS:GO can change after certain events -> this kills our table updates
        /// So we just add a new id for this player to the dictionary. getID is not injective! ( f(a) = f(b) a =/= b )
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
