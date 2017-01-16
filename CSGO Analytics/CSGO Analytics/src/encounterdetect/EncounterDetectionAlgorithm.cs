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

    public class EncounterDetectionAlgorithm
    {
        //
        // VARIABLES AND CONSTANTS
        //

        // Timeouts in seconds.
        private const float TAU = 20;
        private const float ENCOUNTER_TIMEOUT = 20;
        private const float WEAPONFIRE_VICTIMSEARCH_TIMEOUT = 20; // Time after which a Hurt Event has no relevance to a weaponfire event
        private const float PLAYERHURT_WEAPONFIRESEARCH_TIMEOUT = 4;
        private const float PLAYERHURT_DAMAGEASSIST_TIMEOUT = 4;

        private const float FIRE_ATTRACTION_RANGE = 200;
        private const float FIRE_SUPPORTRANGE = 200;
        private const float DECOY_ATTRACTION_RANGE = 200;
        private const float DECOY_ATTRACTION_ANGLE = 10;
        private const float DECOY_SUPPORTRANGE = 200;

        private const float ATTACKRANGE = 500.0f;
        private const float SUPPORTRANGE = 300.0f;

        private const float CLUSTERINGRANGE = 20.0f;

        /// <summary>
        /// Tickrate of the demo this algorithm runs on. 
        /// </summary>
        public float tickrate;

        /// <summary>
        /// All players - communicated by the meta-data - which are participating in this match.
        /// </summary>
        private Player[] players;

        /// <summary>
        /// All living players with the latest data.(position health etc)
        /// </summary>
        private List<Player> livingplayers;

        /// <summary>
        /// All data we have from this match.
        /// </summary>
        private Match match;

        /// <summary>
        /// Simple representation of the map to do basic sight calculations for players
        /// </summary>
        public Map map;


        /// <summary>
        /// Map for CSGO IDS to our own. CSGO is using different IDs for their entities every match.
        /// (Watch out for id changes caused by disconnects/reconnects!!)
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
        private bool[] spotted_table;



        public EncounterDetectionAlgorithm(Gamestate gamestate)
        {
            this.match = gamestate.match;
            this.tickrate = gamestate.meta.tickrate;
            this.players = gamestate.meta.players.ToArray();
            this.livingplayers = players.ToList();

            int ownid = 0;
            foreach (var player in players) // Map all CS Entity IDs to our own table-ids
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
        int fCount = 0;
        int mCount = 0;
        int nCount = 0;
        int uCount = 0;
        int iCount = 0;

        int usCount = 0;
        int ussCount = 0;
        int stCount = 0;
        int dsCount = 0;

        int assistCount = 0;
        int smokeAssistCount = 0;
        int dAssistCount = 0;
        int fsCount = 0;
        int noSpotter = 0;

        private List<Encounter> predecessors = new List<Encounter>();
        /// <summary>
        /// 
        /// </summary>
        public MatchReplay run()
        {

            MatchReplay replay = new MatchReplay();

            generateMap();

            var watch = System.Diagnostics.Stopwatch.StartNew();

            foreach (var round in match.rounds)
            {
                foreach (var tick in round.ticks)
                {
                    foreach (var updatedPlayer in tick.getUpdatedPlayers()) // Update tables if player is alive
                    {
                        int id = getTableID(updatedPlayer);

                        if (!updatedPlayer.isDead()) // TODO: find better solution and is this working(newest data of alive players in this list?) or fetch it through tables?
                        {
                            if (updatedPlayer.isSpotted) stCount++;

                            if (!livingplayers.Contains(updatedPlayer))
                                livingplayers.Add(updatedPlayer);

                            updatePosition(id, updatedPlayer.position.getAsArray());
                            updateFacing(id, updatedPlayer.facing.getAsArray());
                            updateDistance(id);
                            updateSpotted(id, updatedPlayer.isSpotted);

                        }
                        else
                        {
                            updateSpotted(id, false);
                            livingplayers.Remove(updatedPlayer);
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
                        if (Math.Abs(e.tick_id - tick.tick_id) * tickrate / 1000 > ENCOUNTER_TIMEOUT)
                        {
                            open_encounters.Remove(e);
                            closed_encounters.Add(e);
                        }
                    }
                    // NEXT TICK

                } //NO TICKS LEFT -> Round has ended

                // Clear all Events and Queues at end of the round to prevent them from being carried into the next round
                clearRoundData();
            }

            watch.Stop();
            var sec = watch.ElapsedMilliseconds / 1000.0f;

            // Clear match data
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
            Console.WriteLine("\nPlayer is spotted in: " + stCount + " ticks(compare with UpdatedPlayer-Entries)");
            Console.WriteLine("No Spotters found: " + noSpotter);
            Console.WriteLine("Spotters found: " + sfCount);
            Console.WriteLine("Spotted-Events found by Algorithm with Spotrange 500: ");
            Console.WriteLine("UpdatedPlayer spotted someone: " + usCount);
            Console.WriteLine("UpdatedPlayer was spotted: " + ussCount);
            Console.WriteLine("Spotdifferences(only one can see the other): " + ussCount); //TODO: nie im selben tick gegenseitig spotten -> bei playerspotted event in eventsearch

            Console.WriteLine("\nAssist-Supportlinks: " + assistCount);
            Console.WriteLine("DamageAssist-Supportlinks: " + dAssistCount);
            Console.WriteLine("Nade-Supportlinks: ");
            Console.WriteLine("Smoke Supportlinks: " + smokeAssistCount);
            Console.WriteLine("Flashes exploded: " + fCount);
            Console.WriteLine("Flash Supportlinks: " + fsCount);


            Console.WriteLine("\n\n  Encounters found: " + closed_encounters.Count);

            Console.WriteLine("\n  Time to run Algorithm: " + sec + "sec. \n");

            return replay;
        }



        int ipCount = 0;
        /// <summary>
        /// Collect all positions of this replay necessary to build a approximate representation of the map
        /// </summary>
        /// <returns></returns>
        private void generateMap()
        {
            var ps = new List<Vector>();
            foreach (var round in match.rounds)
            {
                foreach (var tick in round.ticks)
                {
                    foreach (var gevent in tick.tickevents)
                    {
                        string weapon = "";
                        // Add Positions interpolated on Hit-Vectors - every point on the route of a hitvector is viable
                        switch (gevent.gameevent)
                        {
                            case "player_hurt":
                                PlayerHurt ph = (PlayerHurt)gevent;
                                weapon = ph.weapon.name;
                                break;
                            case "player_death":
                                PlayerKilled pk = (PlayerKilled)gevent;
                                weapon = pk.weapon.name;
                                break;
                        }
                        //Exclude some nades because they do not deliver correct positions(nades can explode around corners and after a certain time -> hitvector is falsified)
                        if (weapon != WeaponType.HE.ToString() && weapon != WeaponType.Incendiary.ToString() && weapon != "")
                        {
                            var start = gevent.getPositions()[0]; // TODO: change to getPositon("victim");?
                            var end = gevent.getPositions()[1];
                            var ipos = EDMathLibrary.linear_interpolatePositions(start, end, 20);
                            //AddNecessaryRange(ipos, ps);
                            //ps.AddRange(ipos);
                            ipCount += ipos.Count;
                        }
                        //AddNecessaryRange(gevent.getPositions().ToList(), ps);
                        ps.AddRange(gevent.getPositions().ToList());

                    }
                }
            }


            Console.WriteLine("Added " + ipCount + " interpolated Positions");
            Console.WriteLine("\nRegistered Positions for Sightgraph: " + ps.Count);

            this.map = new Map().createMapData(ps);

        }



        /// <summary>
        /// Searches all predecessor encounters of an component. or in other words:
        /// tests if a component is a successor of another encounters component
        /// </summary>
        /// <param name="newcomp"></param>
        /// <returns></returns>
        private List<Encounter> searchPredecessors(CombatComponent newcomp)
        {

            List<Encounter> predecessors = new List<Encounter>();
            foreach (var encounter in open_encounters.Where(e => e.tick_id - newcomp.tick_id <= TAU))
            {
                bool registered = false;
                foreach (var comp in encounter.cs) //Really iterate over components? -> yes because we need c.players
                {
                    // Test if c and comp have at least two players from different teams in common -> Intersection of player lists
                    var intersectPlayers = comp.players.Intersect(newcomp.players).ToList();

                    if (intersectPlayers.Count < 2)
                        continue;

                    var knownteam = intersectPlayers[0].getTeam(); //TODO: kürzer
                    foreach (var p in intersectPlayers)
                    {
                        // Team different to one we know -> this encounter e is a predecessor of the component comp
                        if (knownteam != Team.None && knownteam != p.getTeam())
                        {
                            predecessors.Add(encounter);
                            registered = true; // Stop multiple adding of encounter
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
                cs.AddRange(e.cs); // Watch for OutOfMemoryExceptions here if too many predecessors add up!! 
            }
            var cs_sorted = cs.OrderBy(x => x.tick_id).ToList();
            int encounter_tick_id = cs_sorted.ElementAt(0).tick_id;
            var merged_encounter = new Encounter(encounter_tick_id, cs_sorted);
            merged_encounter.cs.ForEach(comp => comp.parent = merged_encounter); // Set new parent encounter for all components
            return merged_encounter;
        }






        /// <summary>
        /// Queue of all hurtevents(HE) that where fired. Use these to search for a coressponding weaponfire event.
        /// Value is the tick_id as int where the event happend
        /// </summary>
        private Dictionary<PlayerHurt, int> registeredHEQueue = new Dictionary<PlayerHurt, int>();


        /// <summary>
        /// Weaponfire events(WFE) that are waiting for their check.
        /// </summary>
        private Dictionary<WeaponFire, int> pendingWFEQueue = new Dictionary<WeaponFire, int>();


        /// <summary>
        /// Active nades such as smoke and fire nades which have not ended and need to be tested every tick they are effective
        /// </summary>
        private Dictionary<NadeEvents, int> activeNades = new Dictionary<NadeEvents, int>();


        /// <summary>
        /// Current victimcandidates
        /// </summary>
        private List<Player> vcandidates = new List<Player>();


        /// <summary>
        /// Current spottercandidates
        /// </summary>
        private List<Player> scandidates = new List<Player>();




        /// <summary>
        /// Feeds the component with a links resulting from the procedure handling this tick
        /// </summary>
        /// <param name="component"></param>
        /// <param name="g"></param>
        private CombatComponent buildComponent(Tick tick)
        {
            List<Link> links = new List<Link>();

            searchEventbasedSightCombatLinks(tick, links);
            //searchSightbasedCombatLinks(tick, links);

            //searchDistancebasedtLinks(tick, links);
            searchEventbasedLinks(tick, links);

            searchEventbasedNadeSupportlinks(tick, links);

            CombatComponent combcomp = null;
            if (links.Count != 0) //If links have been found
            {
                combcomp = new CombatComponent(tick.tick_id);
                combcomp.links = links;
                combcomp.assignPlayers();
            }

            return combcomp;
        }





        /// <summary>
        /// Search all potential combatlinks based on sight using a spotted variable from the replay data(equivalent to DOTA2 version: player is in attackrange)
        /// </summary>
        /// <param name="tick"></param>
        /// <param name="links"></param>
        private void searchEventbasedSightCombatLinks(Tick tick, List<Link> links)
        {
            foreach (var uplayer in livingplayers)
            {
                var uplayer_id = getTableID(uplayer);
                if (spotted_table[uplayer_id]) //if the player is spotted
                {
                    var potential_spotter = searchSpotterCandidates(uplayer);
                    if (potential_spotter == null)// This should not happend because spotted table is correct and somebody must see the player TODO:!!
                    {
                        noSpotter++; continue;
                    }

                    //TODO: these data is not necessary the latest one because the list livingplayers is not necessary updated with newest player. is it though?
                    links.Add(new Link(potential_spotter, uplayer, LinkType.COMBATLINK, Direction.DEFAULT));
                }
            }

        }



        private void searchSightbasedCombatLinks(Tick tick, List<Link> links)
        {
            //
            // Combatlink Detection based on sight without player_spotted Event:
            //

            // Check for each team if a player can see a player of the other team
            foreach (var player in players.Where(player => player.getTeam() == Team.CT))
            {
                var player_id = getTableID(player);
                var playerpos = new Vector(position_table[player_id]);
                var playerYaw = facing_table[player_id][0];

                foreach (var counterplayer in players.Where(counterplayer => counterplayer.getTeam() == Team.T))
                {
                    var counterplayer_id = getTableID(counterplayer);
                    var counterplayerpos = new Vector(position_table[counterplayer_id]);
                    var counterplayerYaw = facing_table[counterplayer_id][0];

                    bool playerCanSeeCounter = EDMathLibrary.isInFOV(playerpos, playerYaw, counterplayerpos); // Has the updated player spotted someone
                    bool CounterCanSeePlayer = EDMathLibrary.isInFOV(counterplayerpos, counterplayerYaw, playerpos); // Has someone spotted the player

                    //TODO: occlusion not handled
                    if (playerCanSeeCounter)
                    {
                        var link = new Link(player, counterplayer, LinkType.COMBATLINK, Direction.DEFAULT);
                        links.Add(link);
                        usCount++;
                    }
                    if (CounterCanSeePlayer)
                    {
                        var link = new Link(counterplayer, player, LinkType.COMBATLINK, Direction.DEFAULT);
                        links.Add(link);
                        ussCount++;
                    }
                }
            }

        }

        /// <summary>
        /// Algorithm searching links simply on euclid distance
        /// </summary>
        /// <param name="tick"></param>
        /// <param name="links"></param>
        private void searchDistancebasedLinks(Tick tick, List<Link> links)
        {
            foreach (var player in tick.getUpdatedPlayers().Where(player => !player.isDead())) //Updated players contains newest data of players
            {
                for (int i = 0; i < distance_table[getTableID(player)].Length; i++)
                {
                    var actor = livingplayers[getTableID(player)];
                    var reciever = livingplayers[i];
                    var distance = distance_table[getTableID(player)][i];

                    if (distance < ATTACKRANGE) // TODO: Search better ranges -> extra paper?
                    {
                        if (actor.getTeam() != reciever.getTeam())
                            links.Add(new Link(actor, reciever, LinkType.COMBATLINK, Direction.DEFAULT));
                    }
                    else if (distance < SUPPORTRANGE)
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
        private void searchEventbasedLinks(Tick tick, List<Link> links)
        {
            // Events contain players with newest data to this tick
            foreach (var g in tick.tickevents)
            { // Read all gameevents in that tick and build links of them for the component
                switch (g.gameevent)
                {
                    //
                    //  Combatlink-Relevant Events
                    //
                    case "player_hurt":
                        PlayerHurt ph = (PlayerHurt)g;
                        if (ph.actor.getTeam() == ph.victim.getTeam()) continue; // No Team damage
                        links.Add(new Link(ph.actor, ph.victim, LinkType.COMBATLINK, Direction.DEFAULT));

                        handleIncomingHurtEvent(ph, tick.tick_id, links); // CAN PRODUCE SUPPORTLINKS!

                        break;
                    case "player_killed":
                        PlayerKilled pk = (PlayerKilled)g;
                        if (pk.actor.getTeam() == pk.victim.getTeam()) continue; // No Team kills
                        links.Add(new Link(pk.actor, pk.victim, LinkType.COMBATLINK, Direction.DEFAULT));

                        if (pk.assister != null)
                        {
                            links.Add(new Link(pk.assister, pk.actor, LinkType.SUPPORTLINK, Direction.DEFAULT));
                            assistCount++;
                        }

                        break;
                    case "weapon_fire":
                        wfeCount++;
                        WeaponFire wf = (WeaponFire)g;
                        var potential_victim = searchVictimCandidate(wf, tick.tick_id);

                        // No candidate found. Either wait for a incoming playerhurt event or there was no suitable victim
                        if (potential_victim == null)
                            break;
                        wfCount++;
                        links.Add(new Link(wf.actor, potential_victim, LinkType.COMBATLINK, Direction.DEFAULT));

                        break;
                    case "player_spotted":
                        PlayerSpotted ps = (PlayerSpotted)g;
                        var potential_spotter = searchSpotterCandidates(ps.actor);
                        sCount++;
                        if (potential_spotter == null)
                            break;
                        sfCount++;
                        links.Add(new Link(potential_spotter, ps.actor, LinkType.COMBATLINK, Direction.DEFAULT));

                        break;
                }
            }
        }


        /// <summary>
        /// When a hurtevent is registered we want to test if some of our pending weaponfire events match this playerhurt event.
        /// If so we have to insert the link that arises into the right Combatcomponent.
        /// </summary>
        /// <param name="ph"></param>
        /// <param name="tick_id"></param>
        private void handleIncomingHurtEvent(PlayerHurt ph, int tick_id, List<Link> links)
        {
            // For every registered hurt event test ...
            for (int index = registeredHEQueue.Count - 1; index >= 0; index--)
            {
                var item = registeredHEQueue.ElementAt(index);
                var hurtevent = item.Key;
                var htick_id = item.Value;
                int tick_dt = Math.Abs(htick_id - tick_id);

                if (tick_dt * tickrate / 1000 > PLAYERHURT_DAMAGEASSIST_TIMEOUT)
                {
                    registeredHEQueue.Remove(hurtevent); // Check timeout
                    continue;
                }

                // If same victim but different actors from the same team-> damageassist -> multiple teammates attack one enemy
                if (ph.victim.Equals(hurtevent.victim) && !ph.actor.Equals(hurtevent.actor) && ph.actor.getTeam() == hurtevent.actor.getTeam())
                {
                    links.Add(new Link(ph.actor, hurtevent.actor, LinkType.SUPPORTLINK, Direction.DEFAULT));
                    dAssistCount++;
                }
                // If ph.actor hits an enemy while this enemy has hit somebody from p.actors team
                if (ph.victim.Equals(hurtevent.actor) && hurtevent.victim.getTeam() == ph.actor.getTeam())
                {
                    links.Add(new Link(ph.actor, hurtevent.victim, LinkType.SUPPORTLINK, Direction.DEFAULT));
                    dAssistCount++;
                }
            }

            registeredHEQueue.Add(ph, tick_id);

            // Now check all pending weapon fire events if the incoming player hurt event helps them to find a victim
            for (int index = pendingWFEQueue.Count - 1; index >= 0; index--)
            {
                var item = pendingWFEQueue.ElementAt(index);
                var weaponfireevent = item.Key;
                var wftick_id = item.Value;

                int tick_dt = Math.Abs(wftick_id - tick_id);
                if (tick_dt * tickrate / 1000 > PLAYERHURT_WEAPONFIRESEARCH_TIMEOUT)
                {
                    pendingWFEQueue.Remove(weaponfireevent); //Check timeouts
                    continue;
                }

                if (ph.actor.Equals(weaponfireevent.actor) && !ph.actor.isDead() && livingplayers.Contains(weaponfireevent.actor)) // We found a weaponfire event that matches the new playerhurt event
                {
                    Link insertLink = new Link(weaponfireevent.actor, ph.victim, LinkType.COMBATLINK, Direction.DEFAULT); //TODO: only 15 or* 41 links found...seems a bit small

                    foreach (var en in open_encounters) // Search the component in which this link has to be sorted in 
                    {
                        bool inserted = false;
                        foreach (var comp in en.cs.Where(comp => comp.tick_id == wftick_id))
                        {
                            iCount++;
                            comp.links.Add(insertLink);
                            inserted = true;
                            break;
                        }
                        if (inserted) //This should be useless if components and their tick_ids are unique
                            break;
                    }
                    pendingWFEQueue.Remove(weaponfireevent); // Delete the weaponfire event from the queue
                }

            }
        }

        private void searchEventbasedNadeSupportlinks(Tick tick, List<Link> links)
        {
            // Update active nades list with the new tick
            foreach (var g in tick.tickevents)
            {
                switch (g.gameevent)
                {
                    //    
                    //  Supportlink-Relevant Events
                    //
                    case "flash_exploded":
                        FlashNade flash = (FlashNade)g;
                        if (flash.flashedplayers.Count == 0)
                            continue; // The nade flashed noone
                        activeNades.Add(flash, tick.tick_id);
                        fCount++;

                        break;
                    case "firenade_exploded":
                    case "decoy_exploded":
                    case "smoke_exploded":
                        NadeEvents timedNadeStart = (NadeEvents)g;
                        activeNades.Add(timedNadeStart, tick.tick_id);
                        break;
                    case "smoke_ended":
                    case "firenade_ended":
                    case "decoy_ended":
                        NadeEvents timedNadeEnd = (NadeEvents)g;
                        activeNades.Remove(timedNadeEnd);

                        break;
                    default:
                        break;
                }
            }

            //todo: TEST FLASHES!!! no occurencies of links in some demos -> just bad nades?
            updateFlashes(tick); // Flashes dont provide an end-event so we have to figure out when their effect has ended _> we update their effecttime

            searchSupportFlashes(links);
            searchSupportSmokes(links);
            searchSupportDecoys(links);
            searchSupportFires(links);
        }


        #region Decoys and Firenades - NICE TO HAVE!


        private List<Player> registeredNearFire = new List<Player>();

        private List<Player> registeredNearDecoy = new List<Player>();



        private void searchSupportFires(List<Link> links)
        {
            foreach (var fireitem in activeNades.Where(item => item.Key.gameevent == "fire_exploded"))
            {
                // Every player coming closer than a certain range gets registered for potential supportlink activities
                var fireevent = fireitem.Key;
                registeredNearFire.AddRange(livingplayers.Where(player => EDMathLibrary.getEuclidDistance2D(fireevent.position, player.position) < FIRE_ATTRACTION_RANGE && fireevent.actor.getTeam() != player.getTeam()));
            }
        }



        private void searchSupportDecoys(List<Link> links)
        {
            foreach (var decoyitem in activeNades.Where(item => item.Key.gameevent == "decoy_exploded"))
            {
                var decoyevent = decoyitem.Key;
                // Register all enemy players that walked near the grenade -> maybe they thought its a real player
                registeredNearDecoy.AddRange(livingplayers.Where(player => EDMathLibrary.getEuclidDistance2D(decoyitem.Key.position, player.position) < DECOY_ATTRACTION_RANGE && decoyevent.actor.getTeam() != player.getTeam()));
                // Register all enemy players that looked at the grenade in a certain angle -> maybe they thought its a real player
                registeredNearDecoy.AddRange(livingplayers.Where(player => EDMathLibrary.getLoSOffset(player.position, player.facing.yaw, decoyitem.Key.position) < DECOY_ATTRACTION_ANGLE && decoyevent.actor.getTeam() != player.getTeam()));
            }
        }
        #endregion

        /// <summary>
        /// Updates all active flashes. If within a flash is no player which has flashtime(time this player is flashed - flashduration in data) left. The flash has ended.
        /// </summary>
        /// <param name="tick"></param>
        private void updateFlashes(Tick tick)
        {
            foreach (var flashitem in activeNades.Where(item => item.Key.gameevent == "flash_exploded").ToList()) // Make Copy to enable deleting while iterating
            {
                int finishedcount = 0;
                FlashNade flash = (FlashNade)flashitem.Key;
                int ftick = flashitem.Value;
                int tickdt = ftick - tick.tick_id;
                foreach (var player in flash.flashedplayers)
                {
                    if (player.flashedduration >= 0)
                        player.flashedduration -= tickdt * tickrate * 1000; // Count down time
                    else
                        finishedcount++;
                }
                if (finishedcount == flash.flashedplayers.Count)
                    activeNades.Remove(flash);
            }
        }

        /// <summary>
        /// Searches Supportlinks built by flashbang events
        /// </summary>
        /// <param name="links"></param>
        private void searchSupportFlashes(List<Link> links)
        {
            foreach (var f in activeNades.Where(item => item.Key.gameevent == "flash_exploded")) //Update players flashtime and check for links
            {
                FlashNade flash = (FlashNade)f.Key;

                // Each (STILL!) flashed player - as long as it is not a teammate of the actor - is tested for sight on a teammember of the flasher (has flasher prevented sight on one of his teammates) 
                var flashedenemies = flash.flashedplayers.Where(player => player.getTeam() != flash.actor.getTeam() && !player.isDead() && player.flashedduration >= 0);
                if (flashedenemies.Count() == 0)
                    return;

                foreach (var flashedEnemyplayer in flashedenemies) // Every player not in the team of the flasher(sort out all teamflashes)
                {
                    var flashedenemyplayer_id = getTableID(flashedEnemyplayer);
                    var flashedpos = new Vector(position_table[flashedenemyplayer_id]);
                    var flashedYaw = facing_table[flashedenemyplayer_id][0];

                    links.Add(new Link(flash.actor, flashedEnemyplayer, LinkType.COMBATLINK, Direction.DEFAULT)); //TODO: Is a sucessful flash a combatlink?

                    foreach (var teammate in livingplayers.Where(teamate => teamate.getTeam() == flash.actor.getTeam() && flash.actor != teamate))
                    {
                        var teammate_id = getTableID(teammate);
                        var teammatepos = new Vector(position_table[teammate_id]);
                        // Test if a flashed player can see a counterplayer -> create supportlink from nade thrower to counterplayer
                        if (EDMathLibrary.isInFOV(flashedpos, flashedYaw, teammatepos))
                        {
                            // TODO: Teammatedata might not be up to date  + !!! Time is not considered how long player is flashed
                            Link flashsupportlink = new Link(flash.actor, teammate, LinkType.SUPPORTLINK, Direction.DEFAULT);
                            links.Add(flashsupportlink);
                            fsCount++;
                        }
                    }
                }
            }

        }

        /// <summary>
        /// Searches supportlinks built by smokegrenades
        /// </summary>
        /// <param name="supportlinks"></param>
        private void searchSupportSmokes(List<Link> supportlinks)
        {
            foreach (var smokeitem in activeNades.Where(item => item.Key.gameevent == "smoke_exploded"))
            {
                foreach (var counterplayer in livingplayers.Where(player => player.getTeam() != smokeitem.Key.actor.getTeam()))
                {
                    var counterplayer_id = getTableID(counterplayer);
                    var counterplayerpos = new Vector(position_table[counterplayer_id]);
                    var counterplayerYaw = facing_table[counterplayer_id][0];

                    //If a player from the opposing team of the smoke thrower saw into the smoke
                    if (EDMathLibrary.vectorClipsSphere2D(smokeitem.Key.position.x, smokeitem.Key.position.y, 250, counterplayerpos, counterplayerYaw))
                    {
                        //Console.WriteLine("Player " +counterplayer.playername + " saw into the smoke");
                        // Check if he could have seen a player from the thrower team
                        foreach (var teammate in livingplayers.Where(teammate => teammate.getTeam() == smokeitem.Key.actor.getTeam()))
                        {
                            var teammate_id = getTableID(teammate);
                            var teammatepos = new Vector(position_table[teammate_id]);
                            // Test if the player who looked in the smoke can see a player from the oppposing( thrower) team
                            if (EDMathLibrary.isInFOV(counterplayerpos, counterplayerYaw, teammatepos))
                            {
                                //Console.WriteLine("He saw " + teammate.playername + " behind the smoke -> "+nadeevent.actor.playername +" gets an assist" );
                                //TODO: these data is not necessary the latest one because the list livingplayers is not necessary updated with newest player
                                // The actor supported a teammate -> Supportlink
                                Link link = new Link(smokeitem.Key.actor, teammate, LinkType.SUPPORTLINK, Direction.DEFAULT);
                                supportlinks.Add(link);
                                smokeAssistCount++;
                            }
                        }
                    }
                }
            }
        }



        /// <summary>
        /// Searches the player that has most probable attacked another player with the given weapon fire event
        /// This method takes weaponfire events into account which came after a playerhurt event of the weaponfire event actor.
        /// And in most cases a player fires and misses therefore theres a long time between when he might hit the seen opponent because he hides. But still he saw and shot at him. These events are lost here
        /// </summary>
        /// <param name="wf"></param>
        /// <returns></returns>
        private Player searchVictimCandidate(WeaponFire wf, int tick_id)
        {

            for (int index = registeredHEQueue.Count - 1; index >= 0; index--)
            {
                var item = registeredHEQueue.ElementAt(index);
                var hurtevent = item.Key;
                var htick_id = item.Value;

                int tick_dt = Math.Abs(htick_id - tick_id);
                if (tick_dt * tickrate / 1000 > WEAPONFIRE_VICTIMSEARCH_TIMEOUT) // 20 second timeout for hurt events
                {
                    registeredHEQueue.Remove(hurtevent);
                    continue;
                }
                // Watch out for teamdamage -> create wrong combatlinks !!
                // If we find a actor that hurt somebody. this weaponfireevent is likely to be a part of his burst and is therefore a combatlink
                if (wf.actor.Equals(hurtevent.actor) && hurtevent.victim.getTeam() != wf.actor.getTeam() && livingplayers.Contains(hurtevent.victim) && livingplayers.Contains(wf.actor)) //TODO: problem: event players might not be dead in the event but shortly after and then there are links between dead players
                {
                    // Fetch latest data from table because here might be older events which are not up to date
                    var hvictim_id = getTableID(hurtevent.victim);
                    var hvictimpos = new Vector(position_table[hvictim_id]);

                    var wfactor_id = getTableID(wf.actor);
                    var wfactorpos = new Vector(position_table[wfactor_id]);
                    var wfactorYaw = facing_table[wfactor_id][0];
                    /*
                    // Test if an enemy can see our actor
                    if (MathLibrary.isInFOV(wfactorpos, wfactorYaw, hvicitmpos))
                    {
                        candidates.Add(hurtevent.victim);
                        //Order by closest player to determine which is the probablest candidate                    }
                        candidates.OrderBy(candidate => MathLibrary.getEuclidDistance2D(candidate.position, hurtevent.victim.position));
                        break;
                    }*/

                    vcandidates.Add(hurtevent.victim);
                    // Order by closest or by closest los player to determine which is the probablest candidate
                    //vcandidates.OrderBy(candidate => EDMathLibrary.getEuclidDistance2D(hvictimpos, wfactorpos));
                    vcandidates.OrderBy(candidate => EDMathLibrary.getLoSOffset(wfactorpos, wfactorYaw, hvictimpos)); //  Offset = Angle between lineofsight of actor and position of candidate
                    break;
                }
                else // We didnt find a matching hurtevent but there is still a chance for a later hurt event to suite for wf. so we store and try another time
                {
                    pendingWFEQueue.Add(wf, tick_id);
                    break;

                }
            }

            if (vcandidates.Count == 0)
                return null;
            else if (vcandidates.Count == 1)
            {
                var player = vcandidates[0];
                if (player.getTeam() == wf.actor.getTeam()) throw new Exception("No teamfire possible for combatlink creation");
                vcandidates.Clear();
                return player;
            }

            if (vcandidates.Count > 1)
                Console.WriteLine("More than one candidate");
            // Choose the first in the list as we ordered it by Offset (see above)
            var victim = vcandidates[0];
            if (victim.getTeam() == wf.actor.getTeam()) throw new Exception("No teamfire possible for combatlink creation");
            vcandidates.Clear();
            return victim;
        }


        /// <summary>
        /// Searches players who have spotted a certain player
        /// TODO: verify that this method is working
        /// </summary>
        /// <param name="actor"></param>
        /// <returns></returns>
        private Player searchSpotterCandidates(Player actor)
        {
            if (actor.isDead()) return null;

            var actor_id = getTableID(actor);
            var actorpos = new Vector(position_table[actor_id]);

            foreach (var counterplayer in livingplayers.Where(player => player.getTeam() != actor.getTeam()))
            {
                var counterplayer_id = getTableID(counterplayer);
                var counterplayerpos = new Vector(position_table[counterplayer_id]);
                var counterplayerYaw = facing_table[counterplayer_id][0];

                // Test if an enemy can see our actor
                if (EDMathLibrary.isInFOV(counterplayerpos, counterplayerYaw, actorpos))
                {
                    scandidates.Add(counterplayer);
                    scandidates.OrderBy(candidate => EDMathLibrary.getLoSOffset(counterplayerpos, counterplayerYaw, actorpos)); //  Offset = Angle between lineofsight of actor and position of candidate
                }
            }

            if (scandidates.Count == 0)
                return null;

            if (scandidates.Count == 1)
            {
                var player = scandidates[0];
                if (player.getTeam() == actor.getTeam()) throw new Exception("No teamspotting possible");
                scandidates.Clear();
                return player;
            }
            if (scandidates.Count > 1)
            {
                // The one with the shortest distance or the smallest los offset to the actor is the spotter
                //var nearestplayer = scandidates.OrderBy(candidate => EDMathLibrary.getEuclidDistance2D(candidate.position, actor.position)).ToList()[0];
                var nearestplayer = scandidates[0];
                if (nearestplayer.getTeam() == actor.getTeam()) throw new Exception("No teamspotting possible");

                scandidates.Clear();
                return nearestplayer;
            }
            return null;
        }




        //
        //
        // INITALIZATION AND UPDATE METHODS FOR TABLES
        //
        //
        #region Init and Update Tables
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

            spotted_table = new bool[playeramount]; // Table holding bool for every player which tells us if he is spotted in that tick

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
                    distance_table[entityid][i] = (float)EDMathLibrary.getEuclidDistance2D(new Vector(position_table[entityid]), new Vector(position_table[i]));
            }
        }

        private void updateSpotted(int acid, bool spotted)
        {
            spotted_table[acid] = spotted;
        }
        #endregion

        //
        //
        // HELPING METHODS AND ID HANDLING
        //
        //
        #region Helping Methods and ID Handling
        public int getTableID(Player player) // Problem with ID Mapping: Player disconnect or else changes ID of this player
        {
            int id;
            if (mappedPlayerIDs.TryGetValue(player.player_id, out id))
            {
                return id;
            }
            else
            {
                Console.WriteLine("Could not map unkown csid: " + player.player_id + ", on Analytics-ID. Maybe a random CS-ID change occured? -> Key needs update");
#if Debug
                foreach (KeyValuePair<int, int> pair in mappedPlayerIDs)
                    Console.WriteLine("Key: " + pair.Key + " Value: " + pair.Value);
#endif
                handleChangedID(player);
                return getTableID(player);
            }


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
                    mappedPlayerIDs.Add(p.player_id, value);
#if Debug
                foreach (KeyValuePair<int, int> pair in mappedPlayerIDs)
                    Console.WriteLine("Key: " + pair.Key + " Value: " + pair.Value);
#endif
                    return;
                }
            }

            if (value == -99)
            {
                throw new Exception("No suitable ID found in map.");
            }
        }

        /// <summary>
        /// adds only elements of ls if they are not closer than the clusterrange to the master list
        /// </summary>
        /// <param name="ls"></param>
        /// <param name="master"></param>
        private void AddNecessaryRange(List<Vector> ls, List<Vector> master)
        {
            foreach (var p in ls)
            {
                bool add = true;
                foreach (var m in master)
                {
                    if (EDMathLibrary.getEuclidDistance2D(p, m) < CLUSTERINGRANGE)
                    {
                        add = false;
                        break;
                    }
                }
                if (add) master.Add(p);
            }
        }

        /// <summary>
        /// Clear all lists and queues that loose relevance at the end of the round to prevent events from carrying over to the next round
        /// </summary>
        private void clearRoundData()
        {
            registeredNearDecoy.Clear();
            registeredNearFire.Clear();
            activeNades.Clear();
            registeredHEQueue.Clear();
            pendingWFEQueue.Clear();
            scandidates.Clear();
            vcandidates.Clear();
        }
        #endregion

        public void buildHurtClusters()
        {
            var starts = new List<Vector>();
            var ends = new List<Vector>();

            foreach (var round in match.rounds)
            {
                foreach (var tick in round.ticks)
                {
                    foreach (var gevent in tick.tickevents)
                    {
                        var start = gevent.getPositions()[0]; //Change!!
                        var end = gevent.getPositions()[1];
                    }
                }
            }
        }
    }
}
