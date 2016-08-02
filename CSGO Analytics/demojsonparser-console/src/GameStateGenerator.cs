using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using demojsonparser.src.JSON;
using DemoInfo;
using Newtonsoft.Json;
using demojsonparser.src.JSON.objects;
using demojsonparser.src.JSON.events;

namespace demojsonparser.src
{
    public class GameStateGenerator
    {
        static int tick_id = 0;

        const int positioninterval = 8;

        public static JSONMatch match = new JSONMatch();
        public static JSONRound round = new JSONRound();
        public static JSONTick tick = new JSONTick();

        public static void GenerateJSONFile(DemoParser parser, string path)
        {
            //Init lists
            match.rounds = new List<JSONRound>();
            round.ticks = new List<JSONTick>();
            tick.tickevents = new List<JSONGameevent>();

            //Parser to transform DemoParser events to JSON format
            JSONParser jsonparser = new JSONParser(parser, path);
            //JSON holding the whole gamestate
            JSONGamestate gs = new JSONGamestate();
            
            //Variables
            //Use this to differentiate between warmup(maybe even knife rounds in official matches) rounds and real "counting" rounds
            bool hasMatchStarted = false;
            bool hasRoundStarted = false;
            bool hasFreeezEnded = false;

            int round_id = 0;

            List<Player> ingame_players = new List<Player>(); //All players

            //Measure time to roughly check performance
            var watch = System.Diagnostics.Stopwatch.StartNew();

            //Obligatory to use this parser
            parser.ParseHeader();


            
            //Start writing the gamestate object
            parser.MatchStarted += (sender, e) => {
                hasMatchStarted = true;
                //Assign Gamemetadata
                gs.meta = jsonparser.assembleGamemeta();
            };

            //Assign match object
            parser.WinPanelMatch += (sender, e) => {
                if (hasMatchStarted)
                    gs.match = match;
                    hasMatchStarted = false;
            };

            //Start writing a round object
            parser.RoundStart += (sender, e) => {
                if (hasMatchStarted)
                {
                    hasRoundStarted = true;
                    round_id++;
                    round.round_id = round_id;
                }

            };

            //Add round object to match object
            parser.RoundEnd += (sender, e) => {
                if (hasMatchStarted)
                {
                    if (hasRoundStarted) //TODO: maybe round fires false -> do in tickdone event (see github issues of DemoInfo)
                    {
                        round.winner = e.Winner.ToString();
                        match.rounds.Add(round);
                        round = new JSONRound();
                        round.ticks = new List<JSONTick>();
                    }

                    hasRoundStarted = false;

                }

            };



            parser.WeaponFired += (object sender, WeaponFiredEventArgs we) => {
                if (hasMatchStarted)
                    tick.tickevents.Add(jsonparser.assembleWeaponFire(we));
            };


            parser.PlayerKilled += (object sender, PlayerKilledEventArgs e) => {
                if (hasMatchStarted)
                {
                    //the killer is null if vicitm is killed by the world - eg. by falling
                    if (e.Killer != null)
                        tick.tickevents.Add(jsonparser.assemblePlayerKilled(e));
                    
                }

            };

            parser.PlayerHurt += (object sender, PlayerHurtEventArgs e) => {
                if (hasMatchStarted)
                    //the attacker is null if vicitm is damaged by the world - eg. by falling
                    if (e.Attacker != null)
                        tick.tickevents.Add(jsonparser.assemblePlayerHurt(e));
                    
            };

            #region Nadeevents
            //Nade (Smoke Fire Decoy Flashbang and HE) events
            parser.ExplosiveNadeExploded += (object sender, GrenadeEventArgs e) => {
                if (e.ThrownBy != null && hasMatchStarted)
                    tick.tickevents.Add(jsonparser.assembleHEGrenade(e));
            };

            parser.FireNadeStarted += (object sender, FireEventArgs e) => {
                if (e.ThrownBy != null && hasMatchStarted)
                    tick.tickevents.Add(jsonparser.assembleFiregrenade(e));
            };

            parser.FireNadeEnded += (object sender, FireEventArgs e) => {
                if (e.ThrownBy != null && hasMatchStarted)
                    tick.tickevents.Add(jsonparser.assembleFiregrenadeEnded(e));
            };

            parser.SmokeNadeStarted += (object sender, SmokeEventArgs e) => {
                if (e.ThrownBy != null && hasMatchStarted)
                    tick.tickevents.Add(jsonparser.assembleSmokegrenade(e));
            };


            parser.SmokeNadeEnded += (object sender, SmokeEventArgs e) => {
                if (e.ThrownBy != null && hasMatchStarted)
                    tick.tickevents.Add(jsonparser.assembleSmokegrenadeEnded(e));
            };

            parser.DecoyNadeStarted += (object sender, DecoyEventArgs e) => {
                if (e.ThrownBy != null && hasMatchStarted)
                    tick.tickevents.Add(jsonparser.assembleDecoy(e));
            };

            parser.DecoyNadeEnded += (object sender, DecoyEventArgs e) => {
                if (e.ThrownBy != null && hasMatchStarted)
                    tick.tickevents.Add(jsonparser.assembleDecoyEnded(e));
            };

            parser.FlashNadeExploded += (object sender, FlashEventArgs e) => {
                if (e.ThrownBy != null && hasMatchStarted)
                    tick.tickevents.Add(jsonparser.assembleFlashbang(e));
            };

            parser.NadeReachedTarget += (object sender, NadeEventArgs e) => {
                if (e.ThrownBy != null && hasMatchStarted)
                    tick.tickevents.Add(jsonparser.assembleNade(e.NadeType, e.ThrownBy, e.Position, null));
            };

            #endregion

            #region Bombevents
            parser.BombAbortPlant += (object sender, BombEventArgs e) => {
                JSONBomb b = jsonparser.assembleBomb(e);
                b.gameevent = "bomb_abort_plant";
                tick.tickevents.Add(b);
            };

            parser.BombAbortDefuse += (object sender, BombDefuseEventArgs e) => {
                JSONBomb b = jsonparser.assembleBombDefuse(e);
                b.gameevent = "bomb_abort_defuse";
                tick.tickevents.Add(b);
            };

            parser.BombBeginPlant += (object sender, BombEventArgs e) => {
                JSONBomb b = jsonparser.assembleBomb(e);
                b.gameevent = "bomb_begin_plant";
                tick.tickevents.Add(b);
            };

            parser.BombBeginDefuse += (object sender, BombDefuseEventArgs e) => {
                JSONBomb b = jsonparser.assembleBombDefuse(e);
                b.gameevent = "bomb_begin_defuse";
                tick.tickevents.Add(b);
            };

            parser.BombPlanted += (object sender, BombEventArgs e) => {
                JSONBomb b = jsonparser.assembleBomb(e);
                b.gameevent = "bomb_planted";
                tick.tickevents.Add(b);
            };

            parser.BombDefused += (object sender, BombEventArgs e) => {
                JSONBomb b = jsonparser.assembleBomb(e);
                b.gameevent = "bomb_defused";
                tick.tickevents.Add(b);
            };

            parser.BombExploded += (object sender, BombEventArgs e) => {
                JSONBomb b = jsonparser.assembleBomb(e);
                b.gameevent = "bomb_exploded";
                tick.tickevents.Add(b);
            };
            #endregion

            /*
            //Extraevents maybe useful
            parser.RoundFinal += (object sender, RoundFinalEventArgs e) => {
                
            };
            parser.RoundMVP += (object sender, RoundMVPEventArgs e) => {
                
            };
            parser.RoundOfficiallyEnd += (object sender, RoundOfficiallyEndedEventArgs e) => {

            };
            parser.LastRoundHalf += (object sender, LastRoundHalfEventArgs e) => {

            };
            */
            parser.FreezetimeEnded += (object sender, FreezetimeEndedEventArgs e) => {
                hasFreeezEnded = true; //Just capture movement after freezetime has ended
            };

            //Create a tick object
            parser.TickDone += (sender, e) => {
                if (!hasMatchStarted) //Dont count ticks if the game has not started already (dismissing warmup and knife-phase for official matches)
                    return;
                // Every tick save id and time
                // Dumb playerpositions every positioninterval-ticks
                if ((tick_id % positioninterval == 0) && hasFreeezEnded)
                    foreach (var player in parser.PlayingParticipants)
                    {
                        tick.tickevents.Add(jsonparser.assemblePlayerFootstep(player));
                    }

                tick_id++;
            };

            //Parse tickwise and add the resulting tick to the round object
            bool hasnext = true;
            while (hasnext)
            {
                try
                {
                    hasnext = parser.ParseNextTick();
                }
                catch (System.IO.EndOfStreamException e)
                {
                    jsonparser.dump("Problem with tickparsing. Is your .dem a valid (not to old) one?");
                    jsonparser.dump(e.StackTrace);
                    return;
                }

                if (hasRoundStarted)
                {
                    tick.tick_id = tick_id;
                    round.ticks.Add(tick);
                    tick = new JSONTick();
                    tick.tickevents = new List<JSONGameevent>();
                }

            }

            //Dump the complete gamestate object into JSON-file
            jsonparser.dump(gs,false);

            //Work is done.
            jsonparser.stopParser();

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            var sec = elapsedMs / 1000.0f;

            Console.Write("Time(in Seconds): " + sec + "\n");
        }

    }
}