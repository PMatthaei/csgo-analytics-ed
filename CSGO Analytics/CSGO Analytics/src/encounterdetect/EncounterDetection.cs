using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.data.gameobjects;
using CSGO_Analytics.src.encounterdetect;
using CSGO_Analytics.src.encounterdetect.utils;
using CSGO_Analytics.src.math;
using CSGO_Analytics.src.json.jsonobjects;
using CSGO_Analytics.src.data.gameevents;
using CSGO_Analytics.src.export;
using System.Collections;
using FastDBScan;

namespace CSGO_Analytics.src.encounterdetect
{

    public class EncounterDetection
    {
        /// <summary>
        /// Exporter for csv file format
        /// </summary>
        CSVExporter exporter = new CSVExporter();

        //
        // CONSTANTS
        //

        // Timeouts in seconds.
        private const float TAU = 20;
        private const float ENCOUNTER_TIMEOUT = 20;
        private const float WEAPONFIRE_VICTIMSEARCH_TIMEOUT = 5; // Time after which a Hurt Event has no relevance to a weaponfire event
        private const float PLAYERHURT_WEAPONFIRESEARCH_TIMEOUT = 4;
        private const float PLAYERHURT_DAMAGEASSIST_TIMEOUT = 4;

        private const float FIRE_ATTRACTION_RANGE = 200;
        private const float FIRE_SUPPORTRANGE = 200;
        private const float DECOY_ATTRACTION_RANGE = 200;
        private const float DECOY_ATTRACTION_ANGLE = 10;
        private const float DECOY_SUPPORTRANGE = 200;

        private const int CLUSTER_NUM = 6; // TODO: 5-6 seems like a good value-> why? starting from 7 it makes no sense

        //
        // Variable constants. Depend on match
        //
        private double SUPPORTRANGE_AVERAGE;
        private double ATTACKRANGE_AVERAGE;

        /// <summary>
        /// Tickrate of the demo in Hz this algorithm runs on. 
        /// </summary>
        public float tickrate;
        public float ticktime;

        /// <summary>
        /// All players - communicated by the meta-data - which are participating in this match.
        /// </summary>
        private Player[] players;

        /// <summary>
        /// All living players with the latest data.(position health etc)
        /// </summary>
        private HashSet<Player> livingplayers;
        private HashSet<Player> deadplayers;

        private float[][] position_table;
        private float[][] distance_table;

        /// <summary>
        /// Holds every (attackerposition, victimposition) pair of a hitevent with the attackerposition as key
        /// </summary>
        public Hashtable hit_hashtable = new Hashtable();
        private Hashtable assist_hashtable = new Hashtable();

        public Cluster[] attacker_clusters;

        /// <summary>
        /// All data we have from this match.
        /// </summary>
        private Match match;

        /// <summary>
        /// Simple representation of the map to do basic sight calculations for players
        /// </summary>
        public Map map;

        /// <summary>
        /// Dictionary holding the level of player
        /// </summary>
        Dictionary<Player, MapLevel> playerlevels = new Dictionary<Player, MapLevel>();


        /// <summary>
        /// Dictionary for CSGO IDS to our own. CSGO is using different IDs for their entities every match.
        /// (Watch out for id changes caused by disconnects/reconnects!!)
        /// </summary>
        private Dictionary<long, int> playerID_dictionary = new Dictionary<long, int>();


        public EncounterDetection(Gamestate gamestate)
        {
            this.match = gamestate.match;
            this.tickrate = gamestate.meta.tickrate;
            this.ticktime = 1000 / tickrate;
            this.players = gamestate.meta.players.ToArray();
            Console.WriteLine("Start with " + players.Count() + " players.");
            this.livingplayers = new HashSet<Player>(players.ToList());
            printLivingPlayers();
            this.deadplayers = new HashSet<Player>();

            int ownid = 0;
            foreach (var player in players) // Map all CS Entity IDs to our own table-ids
            {
                playerID_dictionary.Add(player.player_id, ownid);
                ownid++;
            }

            initTables(ownid); // Initalize tables for all players(should be 10 for csgo)

            // Gather and prepare data for later 
            preprocessReplayData();


        }

        public Player[] getPlayers()
        {
            return players;
        }

        public List<Encounter> getEncounters()
        {
            return closed_encounters;
        }



        //
        //
        // MAIN ENCOUNTER DETECTION ALGORITHM
        //
        //

        /// <summary>
        /// All currently running, not timed out, encounters
        /// </summary>
        private List<Encounter> open_encounters = new List<Encounter>();

        /// <summary>
        /// Timed out encounters
        /// </summary>
        private List<Encounter> closed_encounters = new List<Encounter>();


        //
        // Encounter Detection Stats - For later analysis. Export to CSV
        //
        #region
        int tickCount = 0;
        int eventCount = 0;

        int predecessorHandledCount = 0;
        int mergeEncounterCount = 0;
        int newEncounterCount = 0;
        int updateEncounterCount = 0;

        int damageAssistCount = 0;
        int killAssistCount = 0;
        int smokeAssistCount_fov = 0;
        int smokeAssistCount_sight = 0;
        int flashAssistCount_fov = 0;
        int flashAssistCount_sight = 0;

        int distancetestCLinksCount = 0;
        int distancetestSLinksCount = 0;
        int clustered_average_distancetestCLinksCount = 0;

        int sighttestCLinksCount = 0;
        int eventtestCLinksCount = 0;
        int eventestSightCLinkCount = 0;
        int spotteventsCount = 0;
        int ticks_with_spotted_playersCount = 0;
        int spotterFoundCount = 0;
        int noSpotterFoundCount = 0;

        int wfCount = 0;
        int wf_matchedVictimCount = 0;
        int wf_insertCount = 0;

        int flashexplodedCount = 0;
        int flashCLinkCount = 0;
        int flashSLinkCount = 0;

        int no_clustered_distanceCount = 0;
        #endregion

        private List<Encounter> predecessors = new List<Encounter>();

        /// <summary>
        /// 
        /// </summary>
        public MatchEDReplay run()
        {
            MatchEDReplay replay = new MatchEDReplay();

            var watch = System.Diagnostics.Stopwatch.StartNew();
            foreach (var round in match.rounds)
            {
                foreach (var tick in new HashSet<Tick>(round.ticks))
                {
                    tickCount++;
                    eventCount += tick.getTickevents().Count;

                    foreach (var sevent in tick.getServerEvents())
                        Console.WriteLine(sevent.gameevent + " " + sevent.actor);

                    handleServerEvents(tick); // Check if disconnects or reconnects happend in this tick

                    handleBindedPlayers();
                    foreach (var updatedPlayer in tick.getUpdatedPlayers()) // Update tables if player is alive
                    {
                        if (updatedPlayer.isSpotted) ticks_with_spotted_playersCount++;
                        updatePlayer(updatedPlayer);
                        int id = GetTableID(updatedPlayer);
                        updatePosition(id, updatedPlayer.position.getAsArray3D()); // Update position first then distance!!
                        updateDistance(id);
                    }
                    handleDisconnectedPlayers();


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
                        open_encounters.Add(new Encounter(component)); newEncounterCount++;
                    }

                    if (predecessors.Count == 1)
                    {
                        predecessors.ElementAt(0).update(component); updateEncounterCount++;
                    }


                    if (predecessors.Count > 1)
                    {
                        // Remove all predecessor encounters from open encounters because we re-add them as joint_encounter
                        open_encounters.RemoveAll(encounter => { return predecessors.Contains(encounter); });
                        var joint_encounter = join(predecessors); // Merge encounters holding these predecessors
                        joint_encounter.update(component);
                        open_encounters.Add(joint_encounter);
                        mergeEncounterCount++;
                    }

                    predecessors.Clear();

                    // Check encounter timeouts every tick
                    for (int i = open_encounters.Count - 1; i >= 0; i--)
                    {
                        Encounter e = open_encounters[i];
                        if (Math.Abs(e.tick_id - tick.tick_id) * (ticktime / 1000) > ENCOUNTER_TIMEOUT)
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
            predecessorHandledCount = newEncounterCount + updateEncounterCount + mergeEncounterCount;
            Console.WriteLine("Hashed Hurt Events: " + hit_hashtable.Count);
            Console.WriteLine("Hashed Assist Events: " + assist_hashtable.Count);
            Console.WriteLine("\nComponent Predecessors handled: " + predecessorHandledCount);
            Console.WriteLine("New Encounters occured: " + newEncounterCount);
            Console.WriteLine("Encounter Merges occured: " + mergeEncounterCount);
            Console.WriteLine("Encounter Updates occured: " + updateEncounterCount);

            Console.WriteLine("\nWeaponfire-Events total: " + wfCount);
            Console.WriteLine("Weaponfire-Event with matched victims: " + wf_matchedVictimCount);
            Console.WriteLine("Weaponfire-Events inserted into existing components: " + wf_insertCount);

            Console.WriteLine("\nSpotted-Events occured: " + spotteventsCount);
            Console.WriteLine("\nPlayer is spotted in: " + ticks_with_spotted_playersCount + " ticks");
            Console.WriteLine("No Spotters found in: " + noSpotterFoundCount + " ticks");
            Console.WriteLine("Spotters found in: " + spotterFoundCount + " ticks");

            Console.WriteLine("Sightbased Combatlinks: " + sighttestCLinksCount);
            Console.WriteLine("Distance (clustered) Combatlinks: " + clustered_average_distancetestCLinksCount);
            Console.WriteLine("Distance (averaged) Combatlinks: " + (distancetestCLinksCount + distancetestSLinksCount));

            Console.WriteLine("\nAssist-Supportlinks: " + killAssistCount);
            Console.WriteLine("DamageAssist-Supportlinks: " + damageAssistCount);
            Console.WriteLine("Nade-Supportlinks: ");
            Console.WriteLine("Smoke Supportlinks (fov): " + smokeAssistCount_fov);
            Console.WriteLine("Smoke Supportlinks (sight): " + smokeAssistCount_sight);
            Console.WriteLine("Flashes exploded: " + flashexplodedCount);
            Console.WriteLine("Flash Supportlinks (fov): " + flashAssistCount_fov);
            Console.WriteLine("Flash Supportlinks (sight): " + flashAssistCount_sight);
            Console.WriteLine("Flash Combatlinks: " + flashCLinkCount);


            Console.WriteLine("\n\n  Encounters found: " + closed_encounters.Count);

            Console.WriteLine("\n  Time to run Algorithm: " + sec + "sec. \n");

            //
            // Export data to csv
            //
            if (false)
                exportEDDataToCSV(sec);

            return replay;
        }

        private HashSet<Player> illegalplayer = new HashSet<Player>();
        private HashSet<Player> disconnectedplayers = new HashSet<Player>();
        private HashSet<Player> bindedplayers = new HashSet<Player>();
        private Dictionary<string, long> botid_to_steamid = new Dictionary<string, long>();
        private Queue<long> disconnected_ids = new Queue<long>();


        /// <summary>
        /// Handles each player who was binded in that tick
        /// </summary>
        private void handleBindedPlayers()
        {
            foreach (var player in bindedplayers)
                if (player.player_id == 0) // Player is a bot -> map his id on a disconnectedplayer -> we update the player with the botdata
                {
                    botid_to_steamid.Add(player.playername, disconnected_ids.Dequeue());
                    continue;
                }
            bindedplayers.Clear();
        }

        /// <summary>
        /// Handles each player who disconnected in that tick
        /// </summary>
        private void handleDisconnectedPlayers()
        {
            foreach (var player in disconnectedplayers)
                if (player.player_id == 0) // Player is a bot -> when a bot disconnects remove his binding to the players steamid
                {
                    botid_to_steamid.Remove(player.playername);
                    continue;
                }
                else // For disconnected players save their id
                {
                    disconnected_ids.Enqueue(player.player_id);
                }
        }

        /// <summary>
        /// Registeres players that have to be handled that tick
        /// </summary>
        /// <param name="tick"></param>
        private void handleServerEvents(Tick tick)
        {
            foreach (var sevent in tick.getServerEvents())
            {
                var player = sevent.actor;
                switch (sevent.gameevent)
                {
                    case "player_bind":
                        bindedplayers.Add(player);

                        if (disconnectedplayers.Contains(player))
                            disconnectedplayers.Remove(player);
                        break;
                    case "player_disconnected":
                        disconnectedplayers.Add(player);

                        break;
                    default: throw new Exception("Unkown ServerEvent");
                }
            }
        }


        /// <summary>
        /// Update a players with his most recent version. Further keeps track of all living players
        /// </summary>
        /// <param name="toUpdate"></param>
        private void updatePlayer(Player toUpdate)
        {
            if (toUpdate.position == null) throw new Exception("Cannot update with null position");
            if (toUpdate.velocity == null) throw new Exception("Cannot update with null velocity");
            int count = 0;

            foreach (var player in players.ToArray())
            {
                var updateid = toUpdate.player_id;
                if (updateid == 0) // We want to update data from a bot in the name of a disconnected player
                    botid_to_steamid.TryGetValue(toUpdate.playername, out updateid);

                if (player.player_id == updateid) // We found the player with a matching id -> update all changeable values
                {
                    if (toUpdate.isDead() && !deadplayers.Contains(player)) //This player is dead but not in removed from the living -> do so
                    {
                        livingplayers.Remove(player);
                        deadplayers.Add(player);
                    }
                    else //Player is alive -> make sure hes in the living list and update him
                    {
                        if (deadplayers.Contains(player) && !livingplayers.Contains(player)) // Player was dead but lives now -> make sure hes in the right list
                        {
                            deadplayers.Remove(player);
                            livingplayers.Add(player);
                        }

                        player.facing = toUpdate.facing;
                        player.position = toUpdate.position;
                        player.velocity = toUpdate.velocity;
                        player.HP = toUpdate.HP;
                        player.isSpotted = toUpdate.isSpotted;
                        count++;
                    }
                }

                if (count > 1)
                {
                    printLivingPlayers();
                    throw new Exception("More than one player with id living or revive is invalid: " + toUpdate.player_id);
                }
            }

            if (count == 0 && !toUpdate.isDead())
            {
                printLivingPlayers();
                throw new Exception("Player :" + toUpdate + " could not be updated.");
            }

        }


        /// <summary>
        /// Loop through the replay data and collect important data such as positions, hurtevent, averages etc for later calculations.
        /// </summary>
        /// <returns></returns>
        private void preprocessReplayData()
        {
            var ps = new HashSet<EDVector3D>();
            List<double> hurt_ranges = new List<double>();
            List<double> support_ranges = new List<double>();

            #region Collect positions for preprocessing  and build hashtables of events
            foreach (var round in match.rounds)
            {
                foreach (var tick in round.ticks)
                {
                    foreach (var gevent in tick.getTickevents())
                    {
                        switch (gevent.gameevent) //Build hashtables with events we need later
                        {
                            case "player_hurt":
                                PlayerHurt ph = (PlayerHurt)gevent;
                                // Remove Z-Coordinate because we later get keys from clusters with points in 2D space -> hashtable needs keys with 2d data
                                hit_hashtable[ph.actor.position.ResetZ()] = ph.victim.position.ResetZ();
                                hurt_ranges.Add(EDMathLibrary.getEuclidDistance2D(ph.actor.position, ph.victim.position));
                                break;
                            case "player_killed":
                                PlayerKilled pk = (PlayerKilled)gevent;
                                hit_hashtable[pk.actor.position.ResetZ()] = pk.victim.position.ResetZ();
                                hurt_ranges.Add(EDMathLibrary.getEuclidDistance2D(pk.actor.position, pk.victim.position));

                                if (pk.assister != null)
                                {
                                    assist_hashtable[pk.actor.position.ResetZ()] = pk.assister.position.ResetZ();
                                    support_ranges.Add(EDMathLibrary.getEuclidDistance2D(pk.actor.position, pk.assister.position));
                                }
                                break;
                        }

                        foreach (var player in gevent.getPlayers())
                        {
                            var vz = player.velocity.VZ;
                            if (vz == 0) //If player is standing thus not experiencing an acceleration on z-achsis -> TRACK POSITION
                                ps.Add(player.position);
                            else
                                ps.Add(player.position.ChangeZ(-54)); // Player jumped -> Z-Value is false -> correct with jumpheight
                        }

                    }
                }
            }
            #endregion
            Console.WriteLine("\nRegistered Positions for Sightgraph: " + ps.Count);

            // Generate Map
            this.map = MapCreator.createMap(ps);

            // Generate Hurteventclusters
            var dbscan = new KD_DBSCANClustering((x, y) => Math.Sqrt(((x.X - y.X) * (x.X - y.X)) + ((x.Y - y.Y) * (x.Y - y.Y))));
            var clusterset = dbscan.ComputeClusterDbscan(allPoints: hit_hashtable.Keys.Cast<EDVector3D>().ToArray(), epsilon: 150, minPts: 3);
            this.attacker_clusters = new Cluster[clusterset.Count];
            int ind = 0;
            foreach (var clusterdata in clusterset)
            {
                attacker_clusters[ind] = new Cluster(clusterdata);
                ind++;
            }


            foreach (var a in attacker_clusters)
                a.calculateClusterAttackrange(hit_hashtable);
            /*this.attacker_clusters = KMeanClustering.createPositionClusters(hit_hashtable.Keys.Cast<EDVector3D>().ToList(), CLUSTER_NUM, false);
            foreach (var a in attacker_clusters)
                a.calculateClusterAttackrange(hit_hashtable);*/

            ATTACKRANGE_AVERAGE = hurt_ranges.Average();
            SUPPORTRANGE_AVERAGE = support_ranges.Average();

        }

        /// <summary>
        /// Adds an encounter and sorts the list to improve later iteration.
        /// </summary>
        /// <param name="es"></param>
        /// <param name="e"></param>
        private void AddEncounter(List<Encounter> es, Encounter e)
        {
            es.Add(e);
            es.OrderByDescending(encounter => encounter.tick_id);
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
                foreach (var comp in encounter.cs)
                {
                    // Test if c and comp have at least two players from different teams in common -> Intersection of player lists
                    var intersectPlayers = comp.players.Intersect(newcomp.players).ToList();

                    if (intersectPlayers.Count < 2)
                        continue;

                    var knownteam = intersectPlayers[0].getTeam(); //TODO: Shorten
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
        private Encounter join(List<Encounter> predecessors) //TODO: Problem: High Tau increases concated merged encounters -> one big encounter
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
        /// Insert the given link into the component at the given tick_id
        /// </summary>
        /// <param name="tick_id"></param>
        /// <param name="insertlink"></param>
        private void insertLinkIntoComponent(int tick_id, Link insertlink)
        {
            foreach (var en in open_encounters) // Search the component in which this link has to be sorted in 
            {
                bool inserted = false;
                var valid_comps = en.cs.Where(comp => comp.tick_id == tick_id);
                if (valid_comps.Count() > 1)
                    throw new Exception("More than one component at tick :" + tick_id + " existing. Components have to be unique!");
                else if (valid_comps.Count() == 0)
                    continue;

                valid_comps.First().links.Add(insertlink);
                wf_insertCount++;
                inserted = true;

                if (inserted) //This should be useless if components and their tick_ids are unique
                    break;
            }
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
            searchSightbasedSightCombatLinks(tick, links); //First update playerlevels

            //searchClusterDistancebasedLinks(links);
            //searchDistancebasedLinks(links);

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
                if (uplayer.isSpotted) //If the player is spotted search the spotter
                {
                    var potential_spotter = searchSpotterCandidates(uplayer);
                    // This should not happend because spotted table is correct and somebody must have seen the player!!
                    if (potential_spotter == null)
                    {
                        noSpotterFoundCount++;
                        continue;
                    }

                    links.Add(new Link(potential_spotter, uplayer, LinkType.COMBATLINK, Direction.DEFAULT));
                    eventestSightCLinkCount++;
                }
            }

        }


        /// <summary>
        /// Search all combatlinks that are based on pure sight. No events are used here. Just positional data and line of sight
        /// </summary>
        /// <param name="tick"></param>
        /// <param name="links"></param>
        private void searchSightbasedSightCombatLinks(Tick tick, List<Link> links)
        {
            // Update playerlevels before we start using them to search links
            foreach (var p in livingplayers)
            {
                if (playerlevels.ContainsKey(p))
                    playerlevels[p] = map.findPlayerLevel(p);
                else
                    playerlevels.Add(p, map.findPlayerLevel(p));
            }

            // Check for each team if a player can see a player of the other team
            foreach (var player in livingplayers.Where(player => player.getTeam() == Team.CT))
            {
                foreach (var counterplayer in livingplayers.Where(counterplayer => counterplayer.getTeam() != player.getTeam()))
                {
                    var playerlink = checkVisibility(player, counterplayer);
                    var counterplayerlink = checkVisibility(counterplayer, player);
                    if (playerlink != null) links.Add(playerlink);
                    if (counterplayerlink != null) links.Add(counterplayerlink);
                }
            }
        }

        /// <summary>
        /// Checks if p1 can see p2 considering obstacles between them: !! this method can only be used when playerlevels get updated see sightbasedsightcombatlinks
        /// </summary>
        /// <param name="links"></param>
        /// <param name="playerlevels"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        private Link checkVisibility(Player p1, Player p2)
        {
            bool p1FOVp2 = EDMathLibrary.isInFOV(p1.position, p1.facing.Yaw, p2.position); // p2 is in fov of p1
            if (!p1FOVp2) return null; // If false -> no sight from p1 to p2 possible because p2 is not even in the fov of p1 -> no link

            //Level height of p1 and p2
            var p1Height = playerlevels[p1].height;
            var p2Height = playerlevels[p2].height;

            var current_ml = playerlevels[p1];
            var coll_pos = EDMathLibrary.vectorIntersectsMapLevelRect(p1.position, p2.position, current_ml); // Check if the p1`s view is blocked on his level

            //Both players are on same level and a collision with a rect was found -> No free sight -> no link
            if (p1Height == p2Height && coll_pos != null) return null;

            //Both players are on same level and no collision with a rect was found -> Free sight -> no wall no obstacle and no other level obstructs the LOS
            if (p1Height == p2Height && coll_pos == null) { sighttestCLinksCount++; return new Link(p1, p2, LinkType.COMBATLINK, Direction.DEFAULT); }


            //
            // Case: p1 and p2 stand on different levels and p2 is in the FOV of p1
            //

            // Check for tunnels
            var p2coll_pos = EDMathLibrary.vectorIntersectsMapLevelRect(p2.position, p1.position, playerlevels[p2]); // Check if the p2`s view is blocked on his level
            if (p2coll_pos == null && coll_pos == null) // Both players on different levels claim to have free sight on the other one but no level transition was registered -> p1 or p2 is in a tunnel
                return null;

            // All levels that have to see from p1 to p2 -> p1`s LOS clips these levels if he wants to see him
            MapLevel[] clipped_levels = map.getClippedLevels(p1Height, p2Height);
            int current_ml_index = 0;

            while (current_ml_index < clipped_levels.Length)
            {
                var nextlevel = clipped_levels[current_ml_index];

                if (coll_pos == null)
                {
                    coll_pos = EDMathLibrary.vectorIntersectsMapLevelRect(p1.position, p2.position, nextlevel); // TODO: Richtiges maplevel gewählt für test?
                    current_ml_index++;
                }
                else if (coll_pos != null)
                {
                    EDVector3D coll_posn = coll_pos;
                    if (coll_posn == null) throw new Exception("Collision point cannot be null");
                    coll_pos = EDMathLibrary.vectorIntersectsMapLevelRect(coll_posn, p2.position, nextlevel); // TODO: Richtiges maplevel gewählt für test?
                    if (coll_pos == null) // Transition between levels -> search next level
                        current_ml_index++;
                    else
                        return null; // Obstacle found -> abort link search
                }
            }
            // Sight has been free from p1 to p2 so add a combatlink
            sighttestCLinksCount++;
            return new Link(p1, p2, LinkType.COMBATLINK, Direction.DEFAULT);
        }

        /// <summary>
        /// Algorithm searching links simply on euclid distance
        /// </summary>
        /// <param name="tick"></param>
        /// <param name="links"></param>
        private void searchDistancebasedLinks(List<Link> links)
        {
            foreach (var player in livingplayers)
            {
                foreach (var other in livingplayers.Where(p => !p.Equals(player)))
                {
                    var distance = distance_table[GetTableID(player)][GetTableID(other)];

                    if (distance <= ATTACKRANGE_AVERAGE && other.getTeam() != player.getTeam())
                    {
                        links.Add(new Link(player, other, LinkType.COMBATLINK, Direction.DEFAULT));
                        distancetestCLinksCount++;
                    }
                    else if (distance <= SUPPORTRANGE_AVERAGE && other.getTeam() == player.getTeam())
                    {
                        links.Add(new Link(player, other, LinkType.SUPPORTLINK, Direction.DEFAULT));
                        distancetestSLinksCount++;
                    }
                }
            }
        }

        /// <summary>
        /// Algorithm searching links by an attackrange provided by average-attackrange of a cluster
        /// </summary>
        /// <param name="links"></param>
        private void searchClusterDistancebasedLinks(List<Link> links)
        {
            foreach (var player in livingplayers)
            {
                foreach (var other in livingplayers.Where(p => !p.Equals(player)))
                {
                    var distance = distance_table[GetTableID(player)][GetTableID(other)];
                    Cluster playercluster = null;
                    foreach (var c in attacker_clusters) // TODO: Change this if clustercount gets to high. Very slow
                    {
                        if (c.getBoundings().Contains(player.position))
                        {
                            playercluster = c;
                            break;
                        }
                    }
                    if (playercluster == null)
                    {
                        no_clustered_distanceCount++;
                        continue; // No Cluster found
                    }

                    var attackrange = playercluster.cluster_attackrange;
                    if (distance <= attackrange && other.getTeam() != player.getTeam())
                    {
                        links.Add(new Link(player, other, LinkType.COMBATLINK, Direction.DEFAULT));
                        clustered_average_distancetestCLinksCount++;
                    }
                    // NO SUPPORTLINKS POSSIBLE WITH THIS METHOD BECAUSE NO SUPPORT CLUSTERS EXISTING
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
            foreach (var g in tick.getTickevents())
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
                        eventtestCLinksCount++;
                        break;
                    case "player_killed":
                        PlayerKilled pk = (PlayerKilled)g;
                        if (pk.actor.getTeam() == pk.victim.getTeam()) continue; // No Team kills
                        links.Add(new Link(pk.actor, pk.victim, LinkType.COMBATLINK, Direction.DEFAULT));

                        if (pk.assister != null)
                        {
                            links.Add(new Link(pk.assister, pk.actor, LinkType.SUPPORTLINK, Direction.DEFAULT));
                            killAssistCount++;
                        }
                        eventtestCLinksCount++;

                        break;
                    case "weapon_fire":
                        wfCount++;
                        WeaponFire wf = (WeaponFire)g;
                        var potential_victim = searchVictimCandidate(wf, tick.tick_id);

                        // No candidate found. Either wait for a incoming playerhurt event or there was no suitable victim
                        if (potential_victim == null)
                            break;
                        wf_matchedVictimCount++;
                        links.Add(new Link(wf.actor, potential_victim, LinkType.COMBATLINK, Direction.DEFAULT));
                        eventtestCLinksCount++;

                        break;
                    case "player_spotted":
                        PlayerSpotted ps = (PlayerSpotted)g;
                        var potential_spotter = searchSpotterCandidates(ps.actor);
                        spotteventsCount++;
                        if (potential_spotter == null)
                            break;
                        spotterFoundCount++;
                        links.Add(new Link(potential_spotter, ps.actor, LinkType.COMBATLINK, Direction.DEFAULT));
                        eventtestCLinksCount++;

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
            for (int index = registeredHEQueue.Count - 1; index >= 0; index--)
            {
                var item = registeredHEQueue.ElementAt(index);
                var hurtevent = item.Key;
                var htick_id = item.Value;
                int tick_dt = Math.Abs(htick_id - tick_id);

                if (tick_dt * (ticktime / 1000) > PLAYERHURT_DAMAGEASSIST_TIMEOUT)
                {
                    registeredHEQueue.Remove(hurtevent); // Check timeout
                    continue;
                }

                // If same victim but different actors from the same team-> damageassist -> multiple teammates attack one enemy
                if (ph.victim.Equals(hurtevent.victim) && !ph.actor.Equals(hurtevent.actor) && ph.actor.getTeam() == hurtevent.actor.getTeam())
                {
                    links.Add(new Link(ph.actor, hurtevent.actor, LinkType.SUPPORTLINK, Direction.DEFAULT));
                    damageAssistCount++;
                }
                // If ph.actor hits an enemy while this enemy has hit somebody from p.actors team
                if (ph.victim.Equals(hurtevent.actor) && hurtevent.victim.getTeam() == ph.actor.getTeam())
                {
                    links.Add(new Link(ph.actor, hurtevent.victim, LinkType.SUPPORTLINK, Direction.DEFAULT));
                    damageAssistCount++;
                }
            }

            registeredHEQueue.Add(ph, tick_id);

            for (int index = pendingWFEQueue.Count - 1; index >= 0; index--)
            {
                var item = pendingWFEQueue.ElementAt(index);
                var weaponfireevent = item.Key;
                var wftick_id = item.Value;

                int tick_dt = Math.Abs(wftick_id - tick_id);
                if (tick_dt * (ticktime / 1000) > PLAYERHURT_WEAPONFIRESEARCH_TIMEOUT)
                {
                    pendingWFEQueue.Remove(weaponfireevent); //Check timeout
                    continue;
                }

                if (ph.actor.Equals(weaponfireevent.actor) && !ph.actor.isDead() && livingplayers.Contains(weaponfireevent.actor)) // We found a weaponfire event that matches the new playerhurt event
                {
                    Link insertlink = new Link(weaponfireevent.actor, ph.victim, LinkType.COMBATLINK, Direction.DEFAULT);
                    eventtestCLinksCount++;

                    insertLinkIntoComponent(wftick_id, insertlink);
                    pendingWFEQueue.Remove(weaponfireevent); // Delete the weaponfire event from the queue
                }

            }
        }



        private void searchEventbasedNadeSupportlinks(Tick tick, List<Link> links)
        {
            // Update active nades list with the new tick
            foreach (var g in tick.getTickevents())
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
                        flashexplodedCount++;

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

            updateFlashes(tick); // Flashes dont provide an end-event so we have to figure out when their effect has ended -> we update their effecttime

            searchFlashes(links);
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
                registeredNearDecoy.AddRange(livingplayers.Where(player => EDMathLibrary.getLoSOffset(player.position, player.facing.Yaw, decoyitem.Key.position) < DECOY_ATTRACTION_ANGLE && decoyevent.actor.getTeam() != player.getTeam()));
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
                float tickdt = Math.Abs(ftick - tick.tick_id);
                foreach (var player in flash.flashedplayers)
                {
                    if (player.flashedduration >= 0)
                    {
                        float dtime = tickdt * (ticktime / 1000);
                        player.flashedduration -= dtime; // Count down time
                    }
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
        private void searchFlashes(List<Link> links)
        {
            foreach (var f in activeNades.Where(item => item.Key.gameevent == "flash_exploded")) //Update players flashtime and check for links
            {
                FlashNade flash = (FlashNade)f.Key;

                // Each (STILL!) living flashed player - as long as it is not a teammate of the actor - is tested for sight on a teammember of the flasher (has flasher prevented sight on one of his teammates) 
                var flashedenemies = flash.flashedplayers.Where(player => player.getTeam() != flash.actor.getTeam() && player.flashedduration >= 0 && getLivingPlayer(player) != null);
                if (flashedenemies.Count() == 0)
                    continue;

                foreach (var flashedEnemyplayer in flashedenemies)
                {

                    links.Add(new Link(flash.actor, flashedEnemyplayer, LinkType.COMBATLINK, Direction.DEFAULT)); //Sucessful flash counts as combatlink
                    eventtestCLinksCount++;
                    flashCLinkCount++;

                    foreach (var teammate in livingplayers.Where(teamate => teamate.getTeam() == flash.actor.getTeam() && flash.actor != teamate))
                    {
                        //TODO: better sight test
                        // Test if a flashed player can see a counterplayer -> create supportlink from nade thrower to counterplayer
                        if (EDMathLibrary.isInFOV(flashedEnemyplayer.position, flashedEnemyplayer.facing.Yaw, teammate.position))
                        {
                            Link flashsupportlink = new Link(flash.actor, teammate, LinkType.SUPPORTLINK, Direction.DEFAULT);
                            links.Add(flashsupportlink);
                            flashAssistCount_fov++;
                        }
                        if (checkVisibility(getLivingPlayer(flashedEnemyplayer), teammate) != null)
                        {
                            Link flashsupportlink = new Link(flash.actor, teammate, LinkType.SUPPORTLINK, Direction.DEFAULT);
                            links.Add(flashsupportlink);
                            flashAssistCount_sight++;
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
                    //If a player from the opposing team of the smoke thrower saw into the smoke
                    if (EDMathLibrary.vectorIntersectsSphere2D(smokeitem.Key.position.X, smokeitem.Key.position.Y, 250, counterplayer.position, counterplayer.facing.Yaw))
                    {
                        // Check if he could have seen a player from the thrower team
                        foreach (var teammate in livingplayers.Where(teammate => teammate.getTeam() == smokeitem.Key.actor.getTeam()))
                        {
                            if (checkVisibility(counterplayer, teammate) != null)
                            {
                                // The actor supported a teammate -> Supportlink
                                Link link = new Link(smokeitem.Key.actor, teammate, LinkType.SUPPORTLINK, Direction.DEFAULT);
                                supportlinks.Add(link);
                                smokeAssistCount_sight++;
                            }
                            // Test if the player who looked in the smoke can see a player from the oppposing( thrower) team
                            if (EDMathLibrary.isInFOV(counterplayer.position, counterplayer.facing.Yaw, teammate.position))
                            {
                                // The actor supported a teammate -> Supportlink
                                Link link = new Link(smokeitem.Key.actor, teammate, LinkType.SUPPORTLINK, Direction.DEFAULT);
                                supportlinks.Add(link);
                                smokeAssistCount_fov++;
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
                if (tick_dt * (ticktime / 1000) > WEAPONFIRE_VICTIMSEARCH_TIMEOUT) // 20 second timeout for hurt events
                {
                    registeredHEQueue.Remove(hurtevent);
                    continue;
                }
                // Watch out for teamdamage -> create wrong combatlinks !!
                // If we find a actor that hurt somebody. this weaponfireevent is likely to be a part of his burst and is therefore a combatlink
                //TODO: problem: event players might not be dead in the event but shortly after and then there are links between dead players
                if (wf.actor.Equals(hurtevent.actor) && hurtevent.victim.getTeam() != wf.actor.getTeam() && livingplayers.Contains(hurtevent.victim) && livingplayers.Contains(wf.actor))
                {
                    // Test if an enemy can see our actor
                    if (EDMathLibrary.isInFOV(wf.actor.position, wf.actor.facing.Yaw, hurtevent.victim.position))
                    {
                        vcandidates.Add(hurtevent.victim);
                        // Order by closest or by closest los player to determine which is the probablest candidate
                        //vcandidates.OrderBy(candidate => EDMathLibrary.getEuclidDistance2D(hvictimpos, wfactorpos));
                        vcandidates.OrderBy(candidate => EDMathLibrary.getLoSOffset(wf.actor.position, wf.actor.facing.Yaw, hurtevent.victim.position)); //  Offset = Angle between lineofsight of actor and position of candidate
                        break;
                    }

                }
                else // We didnt find a matching hurtevent but there is still a chance for a later hurt event to suite for wf -> store this event and try another time
                {
                    pendingWFEQueue.Add(wf, tick_id);
                    break;
                }
            }

            if (vcandidates.Count == 0)
                return null;
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

            foreach (var counterplayer in livingplayers.Where(player => player.getTeam() != actor.getTeam()))
            {
                // Test if an enemy can see our actor
                if (EDMathLibrary.isInFOV(counterplayer.position, counterplayer.facing.Yaw, actor.position))
                {
                    scandidates.Add(counterplayer);
                    scandidates.OrderBy(candidate => EDMathLibrary.getLoSOffset(counterplayer.position, counterplayer.facing.Yaw, actor.position)); //  Offset = Angle between lineofsight of actor and position of candidate
                }
            }

            if (scandidates.Count == 0)
                return null;

            var nearestplayer = scandidates[0];
            if (nearestplayer.getTeam() == actor.getTeam()) throw new Exception("No teamspotting possible");
            scandidates.Clear();
            return nearestplayer;
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

            distance_table = new float[playeramount][];
            for (int i = 0; i < distance_table.Length; i++)
            {
                distance_table[i] = new float[playeramount]; // distance between each player
            }
        }


        private void updatePosition(int entityid, float[] newpos)
        {
            for (int i = 0; i < position_table[entityid].Length; i++)
            {
                position_table[entityid][i] = newpos[i];
            }
        }


        private void updateDistance(int entityid)
        {
            for (int i = 0; i < distance_table[entityid].Length; i++)
            {
                if (entityid != i)
                    distance_table[entityid][i] = (float)EDMathLibrary.getEuclidDistance2D(new EDVector3D(position_table[entityid]), new EDVector3D(position_table[i]));
            }
        }
        #endregion

        //
        //
        // HELPING METHODS AND ID HANDLING
        //
        //
        #region Helping Methods and ID Handling

        private void exportEDDataToCSV(float sec)
        {
            exporter.AddRow();
            exporter["Demoname"] = "";
            exporter["Map"] = "";
            exporter["Tickrate"] = tickrate;
            exporter["Runtime in sec"] = sec;
            exporter["Total ticks"] = sec;
            exporter["Observed ticks"] = tickCount;
            exporter["Observed events"] = eventCount;
            exporter["Hurt/Killed-Events"] = hit_hashtable.Count;
            exporter["Encounters found"] = closed_encounters.Count;
            exporter["Sightcombatlink - Sightbased"] = sighttestCLinksCount;
            exporter["Sightcombatlink - Eventbased"] = eventestSightCLinkCount;
            exporter["Combatlinks - Eventbased"] = eventtestCLinksCount;
            exporter["Combatlinks - Distancebased(Average Hurtrange)"] = distancetestCLinksCount;
            exporter["Combatlinks - Distancebased(Clustered Range)"] = clustered_average_distancetestCLinksCount;
            exporter["Supportlinks - Eventbased"] = damageAssistCount + killAssistCount;
            exporter["Supportlinks - Distancebased(Average Hurtrange)"] = distancetestSLinksCount;
            exporter["Supportlinks - Smoke"] = smokeAssistCount_fov;
            exporter["Supportlinks - Flash"] = flashSLinkCount;
            exporter["Supportlinks - Assist"] = killAssistCount;
            exporter["Supportlinks - Damageassist"] = damageAssistCount;
            exporter.ExportToFile("encounter_detection_results.csv");
        }

        public int GetTableID(Player player)
        {
            var updateid = player.player_id;
            if (updateid == 0) // Check if the player is a bot -> get his mapped id from a disconnected player
                botid_to_steamid.TryGetValue(player.playername, out updateid);

            int id;
            if (playerID_dictionary.TryGetValue(updateid, out id))
            {
                return id;
            }
            else
            {
                Console.WriteLine("Could not map unkown CS-ID: " + updateid + " of Player " + player.playername + " on Analytics-ID. CS-ID change occured -> Key needs update");
#if Debug
                foreach (KeyValuePair<long, int> pair in playerID_dictionary)
                    Console.WriteLine("Key: " + pair.Key + " Value: " + pair.Value);
#endif
                handleChangedID(player);
                return GetTableID(player);
            }


        }

        public void ChangeTableIDKey(long fromKey, long toKey)
        {
            try
            {
                int value = playerID_dictionary[fromKey];
                playerID_dictionary.Remove(fromKey);
                playerID_dictionary[toKey] = value;
            }
            catch (Exception e)
            {
                foreach (KeyValuePair<long, int> pair in playerID_dictionary)
                    Console.WriteLine("Key: " + pair.Key + " Value: " + pair.Value);
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
            Console.ReadLine();

        }

        /// <summary>
        /// IDs given from CS:GO can change after certain events -> this kills our table updates
        /// So we just add a new id for this player to the dictionary. getID is not injective! ( f(a) = f(b) a =/= b )
        /// </summary>
        /// <param name="p"></param>
        private void handleChangedID(Player p)
        {
            long changedKey = -99; //Deprecated
            int value = -99;
            for (int i = 0; i < players.Count() - 1; i++)
            {
                if (players[i].playername.Equals(p.playername)) // Find the player in our initalisation array
                {
                    changedKey = players[i].player_id; // The old key we used but which is not up to date
                    value = i; // Our value is always the position in the initalisation playerarray
                    players[i].player_id = p.player_id; //update his old id to the new changed one but only here!
                    playerID_dictionary.Add(p.player_id, value);
#if Debug
                foreach (KeyValuePair<long, int> pair in playerID_dictionary)
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

        /// <summary>
        /// Gets player that is not updated and tell if he is dead.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private Player getLivingPlayer(Player p)
        {
            var players = livingplayers.Where(player => player.player_id == p.player_id);

            if (players.Count() == 1)
                return players.First();
            else
                return null;
        }

        private void printLivingPlayers()
        {
            foreach (var p in livingplayers)
                Console.WriteLine(p);

            foreach (KeyValuePair<long, int> pair in playerID_dictionary)
                Console.WriteLine("CS ID Key: " + pair.Key + " TableID Value: " + pair.Value);
        }
        #endregion
    }
}
