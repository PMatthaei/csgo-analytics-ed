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
using CSGO_Analytics.src.data.exceptions;
using CSGO_Analytics.src.data.utils;
using System.Collections;
using FastDBScan;
using log4net;


namespace CSGO_Analytics.src.encounterdetect
{

    public class EncounterDetection
    {
        private const bool exportingEnabled = true;
        /// <summary>
        /// Exporter for csv file format
        /// </summary>
        private CSVExporter exporter = new CSVExporter();


        //
        // CONSTANTS
        //

        // Timeouts in seconds.
        private const float TAU = 20;                                   // Time after which a encounter is not a predecessor anymore
        private const float ENCOUNTER_TIMEOUT = 20;                     // Time after which a Encounter ends if he is not extended before
        private const float WEAPONFIRE_VICTIMSEARCH_TIMEOUT = 5;        // Time after which a vicitim is not suitable for a weapon fire event
        private const float PLAYERHURT_WEAPONFIRESEARCH_TIMEOUT = 4;    // Time after which a a player hurt event is not suitable for a weapon fire event
        private const float PLAYERHURT_DAMAGEASSIST_TIMEOUT = 4;        // Time after which a player hurt event is not suitable for a damage assist

        //
        // Variables constant for the hole match
        //
        /// <summary>
        /// Average of all eventbased supports (player killed events with assister - distance assister and actor)
        /// </summary>
        private double SUPPORTRANGE_AVERAGE;

        /// <summary>
        /// Average of all eventbased combats (player killed and hurt events)
        /// </summary>
        private double ATTACKRANGE_AVERAGE;

        /// <summary>
        /// Tickrate of the demo this algorithm runs on in Hz. 
        /// </summary>
        public float tickrate;

        /// <summary>
        /// Ticktime of the demo in ms. 
        /// </summary>
        public float ticktime;

        /// <summary>
        /// All players - communicated by the meta-data - which are participating in this match. Get updated every tick.
        /// </summary>
        private Player[] players;

        /// <summary>
        /// Holds every (attackerposition, victimposition) pair of a hitevent with the attackerposition as key
        /// </summary>
        public Hashtable hit_hashtable = new Hashtable();

        /// <summary>
        /// Holds every (assister, assisted) pair of a playerdeath event with a assister
        /// </summary>
        public Hashtable assist_hashtable = new Hashtable();

        /// <summary>
        /// Holds every (assister, assisted) pair of a playerdeath event with a assister
        /// </summary>
        public Hashtable damage_assist_hashtable = new Hashtable();

        /// <summary>
        /// All Clusters of attackpositions
        /// </summary>
        public AttackerCluster[] attacker_clusters;

        /// <summary>
        /// Simple representation of the map to do basic sight calculations for players
        /// </summary>
        public Map map;
        private MapMetaData mapmeta;

        /// <summary>
        /// Dictionary holding the level of player
        /// </summary>
        public Dictionary<long, MapLevel> playerlevels = new Dictionary<long, MapLevel>();






        /// <summary>
        /// All data we have from this match.
        /// </summary>
        private Match match;

        public EncounterDetection(Gamestate gamestate, MapMetaData mapmeta)
        {
            this.match = gamestate.match;
            this.tickrate = gamestate.meta.tickrate;
            this.ticktime = 1000 / tickrate;
            this.players = gamestate.meta.players.ToArray();
            Console.WriteLine("Start with " + players.Count() + " players.");
            printplayers();

            this.mapmeta = mapmeta;

            // Gather and prepare data for later 
            preprocessReplayData();

        }







        //
        //
        // MAIN ENCOUNTER DETECTION ALGORITHM
        //
        //

        /// <summary>
        /// All currently active - not timed out - encounters
        /// </summary>
        private List<Encounter> open_encounters = new List<Encounter>();

        /// <summary>
        /// Timed out encounters
        /// </summary>
        private List<Encounter> closed_encounters = new List<Encounter>();

        /// <summary>
        /// Currently detected predecessors of a encounter
        /// </summary>
        private List<Encounter> predecessors = new List<Encounter>();



        //
        // Encounter Detection Stats - For later analysis. Export to CSV
        //
        #region Stats
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
        int isSpotted_playersCount = 0;
        int spotterFoundCount = 0;
        int noSpotterFoundCount = 0;

        int wfCount = 0;
        int wf_matchedVictimCount = 0;
        int wf_insertCount = 0;

        int flashexplodedCount = 0;
        int flashCLinkCount = 0;
        int flashSLinkCount = 0;

        int no_clustered_distanceCount = 0;

        float totalencountertime = 0;
        float totalgametime = 0;
        int damageencountercount = 0;
        int killencountercount = 0;
        #endregion



        /// <summary>
        /// 
        /// </summary>
        public MatchReplay detectEncounters()
        {
            MatchReplay replay = new MatchReplay();

            //Total gametime
            var mintick = match.rounds.First().ticks.Min(tick => tick.tick_id);
            var maxtick = match.rounds.Last().ticks.Max(tick => tick.tick_id);
            totalgametime = (maxtick - mintick) * ticktime / 1000;

            var watch = System.Diagnostics.Stopwatch.StartNew();
            foreach (var round in match.rounds)
            {
                foreach (var tick in new HashSet<Tick>(round.ticks))
                {
                    tickCount++;
                    eventCount += tick.getTickevents().Count;

                    handleServerEvents(tick); // Check if disconnects or reconnects happend in this tick

                    handleBindings();

                    foreach (var updatedPlayer in tick.getUpdatedPlayers()) // Update tables if player is alive
                    {
                        if (updatedPlayer.isSpotted) isSpotted_playersCount++;
                        updatePlayer(updatedPlayer);
                    }


                    handleDisconnects();

                    CombatComponent component = buildComponent(tick);

                    replay.insertReplaydata(tick, component); // Save the tick with its component for later replaying. 

                    if (component == null) // No component in this tick
                        continue;

                    //
                    // Everything after here is just sorting components into encounters (use component.parent to identify to which encounter it belongs)
                    //
                    predecessors = searchPredecessors(component); // Check if this component has predecessors

                    if (predecessors.Count == 0)
                    {
                        open_encounters.Add(new Encounter(component)); newEncounterCount++;
                    }

                    if (predecessors.Count == 1)
                    {
                        predecessors[0].update(component); updateEncounterCount++;
                    }

                    if (predecessors.Count > 1)
                    {
                        // Remove all predecessor encounters from open encounters because we re-add them as joint_encounter
                        open_encounters.RemoveAll(encounter =>  predecessors.Contains(encounter));
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
                        if (Math.Abs(e.getLatestTick() - tick.tick_id) * (ticktime / 1000) > ENCOUNTER_TIMEOUT)
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

            // Calculate encounter stats

            foreach(var encounter in closed_encounters)
            {
                totalencountertime += (encounter.getTickRange() * ticktime / 1000);
                if(encounter.isDamageEncounter())
                    damageencountercount++;
                if(encounter.isKillEncounter())
                   killencountercount++;

            }


            // Dump stats to console
            predecessorHandledCount = newEncounterCount + updateEncounterCount + mergeEncounterCount;
            Console.WriteLine("Hashed Hurt Events: " + hit_hashtable.Count);
            Console.WriteLine("Hashed Kill-Assist Events: " + assist_hashtable.Count);
            Console.WriteLine("Hashed Damage-Assist Events: " + damage_assist_hashtable.Count);
            Console.WriteLine("\nComponent Predecessors handled: " + predecessorHandledCount);
            Console.WriteLine("New Encounters occured: " + newEncounterCount);
            Console.WriteLine("Encounter Merges occured: " + mergeEncounterCount);
            Console.WriteLine("Encounter Updates occured: " + updateEncounterCount);

            Console.WriteLine("\nWeaponfire-Events total: " + wfCount);
            Console.WriteLine("Total Weaponfire-Events matched: " + (wf_matchedVictimCount + wf_insertCount));

            Console.WriteLine("Weaponfire-Event first time matched victims: " + wf_matchedVictimCount);
            Console.WriteLine("Weaponfire-Events victims inserted into existing components: " + wf_insertCount);

            Console.WriteLine("\nSpotted-Events occured: " + spotteventsCount);
            Console.WriteLine("\nPlayer is spotted in: " + isSpotted_playersCount + " ticks");
            Console.WriteLine("No Spotters found in: " + noSpotterFoundCount + " ticks");
            Console.WriteLine("Spotters found in: " + spotterFoundCount + " ticks");

            Console.WriteLine("Sightbased Combatlinks: " + sighttestCLinksCount);
            Console.WriteLine("Sightbased Combatlinks Error: " + errorcount);
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
            if (exportingEnabled)
                exportEDDataToCSV(sec);

            return replay;
        }

        /// <summary>
        /// All currently disconnected players
        /// </summary>
        private HashSet<Player> disconnectedplayers = new HashSet<Player>();

        /// <summary>
        /// All players that await binding
        /// </summary>
        private HashSet<Player> bindedplayers = new HashSet<Player>();

        /// <summary>
        /// Matches bot name to the ID of the player this bot is replaceing
        /// </summary>
        private Dictionary<string, long> botid_to_steamid = new Dictionary<string, long>();

        /// <summary>
        /// ID-Queue of disconnected players - just working if players who disconnect first rejoin first!
        /// </summary>
        private Queue<long> disconnected_ids = new Queue<long>();

        private void handleBindings()
        {
            foreach (var player in bindedplayers)
            {
                if (player.player_id == 0) // Player is a bot -> map his id on a disconnectedplayer -> we update the player with the botdata
                {
                    if (disconnected_ids.Count == 0) throw new PlayerBindingException();
                    botid_to_steamid.Add(player.playername, disconnected_ids.Dequeue());
                    continue;
                }

                if (disconnectedplayers.Contains(player))
                    disconnectedplayers.Remove(player);
                else throw new PlayerBindingException(); // The player did not disconnect before. -> he missed the first round
            }
            bindedplayers.Clear();
        }

        private void handleDisconnects()
        {
            foreach (var player in disconnectedplayers)
            {
                if (player.player_id == 0) // Player is a bot -> when a bot disconnects remove his binding to the players steamid
                {
                    botid_to_steamid.Remove(player.playername);
                    continue;
                }
            }
        }

        /// <summary>
        /// Registeres players which have to be handled because of a connection problem
        /// </summary>
        /// <param name="tick"></param>
        private void handleServerEvents(Tick tick)
        {
            foreach (var sevent in tick.getServerEvents())
            {
                Console.WriteLine(sevent.gameevent + " " + sevent.actor);

                var player = sevent.actor;
                switch (sevent.gameevent)
                {
                    case "player_bind":
                        bindedplayers.Add(player);

                        break;
                    case "player_disconnected":
                        disconnectedplayers.Add(player);
                        if (player.player_id != 0)
                            disconnected_ids.Enqueue(player.player_id);
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
            int idcount = 0;

            foreach (var player in players)
            {
                var updateid = toUpdate.player_id;
                if (updateid == 0) // We want to update data from a bot in the name of a disconnected player
                    botid_to_steamid.TryGetValue(toUpdate.playername, out updateid);

                if (player.player_id == updateid) // We found the player with a matching id -> update all changeable values
                {
                    idcount++;
                    if (toUpdate.isDead()) // && !deadplayers.Contains(player)) //This player is dead but not in removed from the living -> do so
                    {
                        player.HP = toUpdate.HP;
                    }
                    else //Player is alive -> make sure hes in the living list and update him
                    {
                        player.facing = toUpdate.facing;
                        player.position = toUpdate.position;
                        player.velocity = toUpdate.velocity;
                        player.HP = toUpdate.HP;
                        player.isSpotted = toUpdate.isSpotted;
                    }
                }

                if (idcount > 1)
                {
                    printplayers();
                    throw new Exception("More than one player with id living or revive is invalid: " + toUpdate.player_id);
                }
            }

            if (idcount == 0) throw new Exception("No player with id: " + toUpdate.player_id + " found.");

        }

        /// <summary>
        /// Loop through the replay data and collect important data such as positions, hurtevent, averages etc for later calculations.
        /// Alternatively load a file with this information. TODO
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
                                continue;
                            case "player_killed":
                                PlayerKilled pk = (PlayerKilled)gevent;
                                hit_hashtable[pk.actor.position.ResetZ()] = pk.victim.position.ResetZ();
                                hurt_ranges.Add(EDMathLibrary.getEuclidDistance2D(pk.actor.position, pk.victim.position));

                                if (pk.assister != null)
                                {
                                    assist_hashtable[pk.actor.position.ResetZ()] = pk.assister.position.ResetZ();
                                    support_ranges.Add(EDMathLibrary.getEuclidDistance2D(pk.actor.position, pk.assister.position));
                                }
                                continue;
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

            // Generate 
            MapCreator.mapdata_height = (int)mapmeta.height;
            MapCreator.mapdata_width = (int)mapmeta.width;
            MapCreator.pos_x = (int)mapmeta.mapcenter_x;
            MapCreator.pos_y = (int)mapmeta.mapcenter_y;
            this.map = MapCreator.createMap(mapmeta,ps);

            if (support_ranges.Count != 0)
                ATTACKRANGE_AVERAGE = hurt_ranges.Average();
            if(support_ranges.Count != 0)
                SUPPORTRANGE_AVERAGE = support_ranges.Average();

            // Generate Hurteventclusters
            #region Old Cluseralgorithms
            /*
            var dbscan = new KD_DBSCANClustering((x, y) => Math.Sqrt(((x.X - y.X) * (x.X - y.X)) + ((x.Y - y.Y) * (x.Y - y.Y))));
            var clusterset = dbscan.ComputeClusterDbscan(allPoints: hit_hashtable.Keys.Cast<EDVector3D>().ToArray(), epsilon: 150, minPts: 3);

            this.attacker_clusters = new Cluster[clusterset.Count];
            int ind = 0;
            foreach (var clusterdata in clusterset)
            {
                attacker_clusters[ind] = new Cluster(clusterdata);
                ind++;
            } 

            this.attacker_clusters = KMeanClustering.createPositionClusters(hit_hashtable.Keys.Cast<EDVector3D>().ToList(), CLUSTER_NUM, false); */
            #endregion

            var leader = new LEADERClustering((float)ATTACKRANGE_AVERAGE);
            var attackerclusters = new List<AttackerCluster>();
            foreach (var cluster in leader.clusterData(hit_hashtable.Keys.Cast<EDVector3D>().ToList()))
            {
                var attackcluster = new AttackerCluster(cluster.data.ToArray());
                attackcluster.calculateClusterAttackrange(hit_hashtable);
                attackerclusters.Add(attackcluster);
            }
            this.attacker_clusters = attackerclusters.ToArray();
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
        private Encounter join(List<Encounter> predecessors)
        {
            List<CombatComponent> cs = new List<CombatComponent>();
            foreach (var encounter in predecessors)
            {
                cs.AddRange(encounter.cs); // Watch for OutOfMemoryExceptions here if too many predecessors add up -> high tau -> one big encounter!! 
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
            foreach (var en in open_encounters) // Search the component in a encounter in which this link has to be sorted in 
            {
                bool inserted = false;
                var valid_comps = en.cs.Where(comp => comp.tick_id == tick_id); // Get the component with this tickid
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
        /// Value is the tick_id as int where the event happend.
        /// </summary>
        private Dictionary<PlayerHurt, int> registeredHEQueue = new Dictionary<PlayerHurt, int>();


        /// <summary>
        /// Weaponfire events(WFE) that are waiting for their check.
        /// Value is the tick_id as int where the event happend.
        /// </summary>
        private Dictionary<WeaponFire, int> pendingWFEQueue = new Dictionary<WeaponFire, int>();


        /// <summary>
        /// Active nades such as smoke and fire nades which have not ended and need to be tested every tick they are effective
        /// Value is the tick_id as int where the event happend.
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

            //searchEventbasedSightCombatLinks(tick, links);
            //searchSightbasedSightCombatLinks(tick, links); //First update playerlevels

            //searchClusterDistancebasedLinks(links); // With clusterbased distance
            //searchDistancebasedLinks(links); // With average distance 

            searchEventbasedLinks(tick, links);
            //searchEventbasedNadeSupportlinks(tick, links);

            CombatComponent combcomp = null;
            if (links.Count != 0) //If links have been found
            {
                links.RemoveAll(link => link == null); //If illegal links have been built they are null -> remove them
                combcomp = new CombatComponent(tick.tick_id, links);
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
            foreach (var uplayer in players.Where(p => !p.isDead()))
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
            foreach (var p in players.Where(counterplayer => !counterplayer.isDead()))
            {
                if (playerlevels.ContainsKey(p.player_id))
                    playerlevels[p.player_id] = map.findLevelFromPlayer(p);
                else
                    playerlevels.Add(p.player_id, map.findLevelFromPlayer(p));
            }

            // Check for each team if a player can see a player of the other team
            foreach (var player in players.Where(player => !player.isDead() && player.getTeam() == Team.CT))
            {
                foreach (var counterplayer in players.Where(counterplayer => !counterplayer.isDead() && counterplayer.getTeam() != Team.CT))
                {
                    var playerlink = checkVisibility(player, counterplayer);
                    var counterplayerlink = checkVisibility(counterplayer, player);
                    if (playerlink != null) links.Add(playerlink);
                    if (counterplayerlink != null) links.Add(counterplayerlink);
                }
            }
        }


        private static int errorcount = 0;
        /// <summary>
        /// Checks if p1 can see p2 considering obstacles between them: !! this method can only be used when playerlevels get updated see sightbasedsightcombatlinks
        /// </summary>
        /// <param name="links"></param>
        /// <param name="playerlevels"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        private Link checkVisibility(Player p1, Player p2)
        {
            // Console.WriteLine("New test");
            bool p1FOVp2 = EDMathLibrary.isInFOV(p1.position, p1.facing.Yaw, p2.position); // p2 is in fov of p1
            if (!p1FOVp2) return null; // If false -> no sight from p1 to p2 possible because p2 is not even in the fov of p1 -> no link

            //Level height of p1 and p2
            var p1Height = playerlevels[p1.player_id].height;
            var p2Height = playerlevels[p2.player_id].height;
            //Console.WriteLine(p1Height+ " to " + p2Height);

            var current_maplevel = playerlevels[p1.player_id];

           // var coll_pos = EDMathLibrary.LOSIntersectsMapBresenham(p1.position, p2.position, current_maplevel); // Check if the p1`s view is blocked on his level
            var coll_pos = EDMathLibrary.LOSIntersectsMap(p1.position, p2.position, current_maplevel); // Check if the p1`s view is blocked on his level
            //if(coll_pos == null)Console.WriteLine("Start coll: " + coll_pos);

            //Both players are on same level and a collision with a rect was found -> No free sight -> no link
            if (p1Height == p2Height && coll_pos != null) { return new Link(p1, p2, LinkType.COMBATLINK, Direction.DEFAULT, coll_pos); }

            //Both players are on same level and no collision with a rect was found -> Free sight -> no wall no obstacle and no other level obstructs the LOS
            if (p1Height == p2Height && coll_pos == null) { sighttestCLinksCount++; return new Link(p1, p2, LinkType.COMBATLINK, Direction.DEFAULT, coll_pos); }

            // Check for tunnels
            var p2coll_pos = EDMathLibrary.LOSIntersectsMap(p2.position, p1.position, playerlevels[p2.player_id]); // Check if the p2`s view is blocked on his level
            if (p2coll_pos == null && coll_pos == null && p1Height != p2Height) // Both players on different levels claim to have free sight on the other one but no level transition was registered -> p1 or p2 is in a tunnel
                return null;

            //
            // Case: p1 and p2 stand on different levels and p2 is in the FOV of p1
            //
            throw new Exception("Invalid if just one level");
            //Error occuring -> a gridcell registers two points with different level assigning -> the cell is free on both level -> collision can be null although player is standing in a differnt level
            if (coll_pos == null && p1Height != p2Height) { errorcount++; return null; }

            if (coll_pos == null) throw new Exception("Shit cannot happen");

            // All levels that have to see from p1 to p2 -> p1`s LOS clips these levels if he wants to see him
            MapLevel[] clipped_levels = map.getClippedLevels(p1Height, p2Height);

            for (int i = 0; i < clipped_levels.Length; i++) // Check next levels: p1Height+1, p1Height+2
            {
                if (coll_pos == null) // No collision -> check next level with same line
                    throw new Exception("No null collision alloweed for further testing. Tunnel must have occured");

                var nextlevel = clipped_levels[i];

                if (coll_pos != null) // collision ->  check if a new level is beginning or if there is wall
                {
                    EDVector3D last_coll_pos = coll_pos;
                    coll_pos = EDMathLibrary.LOSIntersectsMap(last_coll_pos, p2.position, nextlevel); // New line from last collision
                    // Free sight before level of p2 was entered through transition -> tunnel
                    if (coll_pos == null && nextlevel.height != p2Height) return null;
                    // Free sight on the last level -> sight was free
                    if (coll_pos == null && nextlevel.height == p2Height) continue;

                    // If a collision was found in the new level which is not our startpoint from the new line -> Transition between levels -> search next level
                    if (coll_pos != null && !coll_pos.Equals(last_coll_pos))
                        continue;
                    // If a collision was found in the new level which is equals to our startpoint from the new line -> Next level has obstacle at same position -> No free sight
                    else if (coll_pos != null && coll_pos.Equals(last_coll_pos))
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
            foreach (var player in players.Where(p => !p.isDead()))
            {
                foreach (var other in players.Where(p => !p.Equals(player) && !p.isDead()))
                {
                    var distance = EDMathLibrary.getEuclidDistance2D(player.position, other.position);

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
            foreach (var player in players.Where(p => !p.isDead()))
            {
                foreach (var other in players.Where(p => !p.Equals(player) && !p.isDead()))
                {
                    var distance = EDMathLibrary.getEuclidDistance2D(player.position, other.position);
                    AttackerCluster playercluster = null;
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
                        var link_ph = new Link(ph.actor, ph.victim, LinkType.COMBATLINK, Direction.DEFAULT);
                        links.Add(link_ph);
                        link_ph.impact = ph.HP_damage + ph.armor_damage;

                        handleIncomingHurtEvent(ph, tick.tick_id, links); // CAN PRODUCE SUPPORTLINKS!
                        eventtestCLinksCount++;
                        break;
                    case "player_killed":
                        PlayerKilled pk = (PlayerKilled)g;
                        if (pk.actor.getTeam() == pk.victim.getTeam()) continue; // No Team kills
                        var link_pk = new Link(pk.actor, pk.victim, LinkType.COMBATLINK, Direction.DEFAULT);
                        link_pk.impact = pk.HP_damage + pk.armor_damage;
                        link_pk.isKill = true;
                        links.Add(link_pk);

                        if (pk.assister != null && pk.assister.getTeam() == pk.actor.getTeam())
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
                        if (potential_victim == null) break;
                        if(wf.actor.getTeam() != potential_victim.getTeam()) break;
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
                    if (!damage_assist_hashtable.ContainsKey(ph.actor.position)) damage_assist_hashtable.Add(ph.actor.position, hurtevent.actor.position);
                    damageAssistCount++;
                }
                // If ph.actor hits an enemy while this enemy has hit somebody from p.actors team
                if (ph.victim.Equals(hurtevent.actor) && hurtevent.victim.getTeam() == ph.actor.getTeam())
                {
                    links.Add(new Link(ph.actor, hurtevent.victim, LinkType.SUPPORTLINK, Direction.DEFAULT));
                    if(!damage_assist_hashtable.ContainsKey(ph.actor.position)) damage_assist_hashtable.Add(ph.actor.position, hurtevent.victim.position);
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

                if (ph.actor.Equals(weaponfireevent.actor) && !ph.actor.isDead() && players.Where(p => !p.isDead()).Contains(weaponfireevent.actor)) // We found a weaponfire event that matches the new playerhurt event
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

        }


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

                    foreach (var teammate in players.Where(teamate => !teamate.isDead() && teamate.getTeam() == flash.actor.getTeam() && flash.actor != teamate))
                    {
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
                foreach (var counterplayer in players.Where(player => !player.isDead() && player.getTeam() != smokeitem.Key.actor.getTeam()))
                {
                    //If a player from the opposing team of the smoke thrower saw into the smoke
                    if (EDMathLibrary.vectorIntersectsSphere2D(smokeitem.Key.position.X, smokeitem.Key.position.Y, 250, counterplayer.position, counterplayer.facing.Yaw))
                    {
                        // Check if he could have seen a player from the thrower team
                        foreach (var teammate in players.Where(teammate => !teammate.isDead() && teammate.getTeam() == smokeitem.Key.actor.getTeam()))
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
                // If we find a actor that hurt somebody. this weaponfireevent is likely to be a part of his burst and is therefore a combatlink
                if (wf.actor.Equals(hurtevent.actor) && hurtevent.victim.getTeam() != wf.actor.getTeam() && players.Where(player => !player.isDead()).Contains(hurtevent.victim) && players.Where(player => !player.isDead()).Contains(wf.actor))
                {
                    // Test if an enemy can see our actor
                    if (EDMathLibrary.isInFOV(wf.actor.position, wf.actor.facing.Yaw, hurtevent.victim.position))
                    {
                        vcandidates.Add(hurtevent.victim);
                        // Order by closest distance or by closest los player to determine which is the probablest candidate
                        //vcandidates.OrderBy(candidate => EDMathLibrary.getEuclidDistance2D(hvictimpos, wfactorpos));
                        vcandidates.OrderBy(candidate => EDMathLibrary.getLOSOffset(wf.actor.position, wf.actor.facing.Yaw, hurtevent.victim.position)); //  Offset = Angle between lineofsight of actor and position of candidate
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

            foreach (var counterplayer in players.Where(player => !player.isDead() && player.getTeam() != actor.getTeam()))
            {
                // Test if an enemy can see our actor
                if (EDMathLibrary.isInFOV(counterplayer.position, counterplayer.facing.Yaw, actor.position))
                {
                    scandidates.Add(counterplayer);
                    scandidates.OrderBy(candidate => EDMathLibrary.getLOSOffset(counterplayer.position, counterplayer.facing.Yaw, actor.position)); //  Offset = Angle between lineofsight of actor and position of candidate
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
            exporter["Gametime"] = totalgametime; 
            exporter["Encountertime"] = totalencountertime;
             exporter["Runtime in sec"] = sec;
            exporter["Observed ticks"] = tickCount;
            exporter["Observed events"] = eventCount;
            exporter["Hurt/Killed-Events"] = hit_hashtable.Count;
            exporter["Encounters found"] = closed_encounters.Count;
            exporter["Damage Encounters"] = damageencountercount; 
            exporter["Kill Encounters"] = killencountercount;
             exporter["Players spotted in ticks"] = isSpotted_playersCount;
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

       
        /// <summary>
        /// Clear all lists and queues that loose relevance at the end of the round to prevent events from carrying over to the next round
        /// </summary>
        private void clearRoundData()
        {
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
            var players = this.players.Where(player => player.player_id == p.player_id);

            if (players.Count() == 1)
                return players.First();
            else
                return null;
        }

        private void printplayers()
        {
            foreach (var p in players)
                Console.WriteLine(p);
        }

        public Player[] getPlayers()
        {
            return players;
        }

        public List<Encounter> getEncounters()
        {
            return closed_encounters;
        }
        #endregion
    }
}
