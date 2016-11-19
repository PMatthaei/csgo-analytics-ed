using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.encounterdetect;
using CSGO_Analytics.src.data.gameobjects;
using CSGO_Analytics.src.math;
using DP = DemoInfoModded;
using Newtonsoft.Json;
using CSGO_Analytics.src.json.jsonobjects;
using CSGO_Analytics.src.json.parser;
namespace CSGO_Analytics.src.views
{
    class AnalyseDemosModel
    {

        private MatchReplay matchreplay;

        //
        // MAP VARIABLES
        //
        private string mapname;
        private double scalefactor_map;
        private double map_width;
        private double map_height;
        private double map_x;
        private double map_y;
        private int mapcenter_x;
        private int mapcenter_y;
        private double scale;
        private int rotate;
        private double zoom;

        //
        // UI VARIABLES
        //
        private double timeslider_min;
        private double timeslider_max;


        //
        // GENERAL
        // 
        private float tickrate;

        public AnalyseDemosModel(Gamestate gamestate)
        {
            matchreplay = new EncounterDetectionAlgorithm(gamestate).run();
            this.tickrate = gamestate.meta.tickrate;
            this.mapname = gamestate.meta.mapname;
            timeslider_min = 0;
            timeslider_max = gamestate.match.rounds.Last().ticks.Last().tick_id;

            MathLibrary.initalizeConstants();
        }
    }
}
