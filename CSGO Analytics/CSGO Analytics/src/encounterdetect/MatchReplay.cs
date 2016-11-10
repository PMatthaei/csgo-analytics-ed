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

        public void insertComponents(Tick tick, CombatComponent comp)
        {
            ticksdata.Add(new Tuple<Tick, CombatComponent>(tick , comp));
        }

        public List<Tuple<Tick, CombatComponent>> getReplayData()
        {
            return ticksdata;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
