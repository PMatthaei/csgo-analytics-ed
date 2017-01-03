using System;
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
    public class MatchReplay : IDisposable
    {
        List<Tuple<Tick, CombatComponent>> ticksdata = new List<Tuple<Tick, CombatComponent>>();
        //Dictionary<Tick, CombatComponent> ticksdata = new Dictionary<Tick, CombatComponent>();

        public void insertData(Tick tick, CombatComponent comp)
        {
            //ticksdata.Add(tick, comp);
            ticksdata.Add(new Tuple<Tick, CombatComponent>(tick, comp));
        }

        public List<Tuple<Tick, CombatComponent>> getTicksFromRound(int round_id)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Return all ticks with same or higher tick_id(inclusive)
        /// </summary>
        /// <param name="tick_id"></param>
        /// <returns></returns>
        public List<Tuple<Tick, CombatComponent>> getTicksFrom(int tick_id)
        //public List<KeyValuePair<Tick,CombatComponent>> getTicksFrom(int tick_id)
        {
            //return ticksdata.Where(t => t.Key.tick_id >= tick_id).ToList();
            return ticksdata.Where(t => t.Item1.tick_id >= tick_id).ToList();
        }

        /// <summary>
        /// Return all ticks with same or lower tick_id(inclusive)
        /// </summary>
        /// <param name="tick_id"></param>
        /// <returns></returns>
        //public List<KeyValuePair<Tick,CombatComponent>> getTicksUntil(int tick_id)
        public List<Tuple<Tick, CombatComponent>> getTicksUntil(int tick_id)
        {
            return ticksdata.Where(t => t.Item1.tick_id <= tick_id).ToList();
            //return ticksdata.Where(t => t.Key.tick_id <= tick_id).ToList();
        }

        //public Dictionary<Tick, CombatComponent> getReplayData()
        public List<Tuple<Tick, CombatComponent>> getReplayData()
        {
            return ticksdata;
        }

        public void Dispose()
        {
            ticksdata = null;
        }
    }
}
