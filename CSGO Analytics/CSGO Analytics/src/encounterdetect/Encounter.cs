using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.data.gameobjects;

namespace CSGO_Analytics.src.encounterdetect
{
    /// <summary>
    /// Graph showing one Encounter at a tick tick_id
    /// </summary>
    class Encounter
    {
        /// <summary>
        /// Components which form this encounter
        /// </summary>
        public List<CombatComponent> cs;

        /// <summary>
        /// Tick in which this encounter arised
        /// </summary>
        public int tick_id;

        /// <summary>
        /// Is this encounter closed 
        /// </summary>
        public bool isClosed;

        /// <summary>
        /// Time to die for this encounter
        /// </summary>
        public float TTD;

        public Encounter(CombatComponent comp)
        {
            this.tick_id = comp.tick_id;
            cs = new List<CombatComponent>();
            cs.Add(comp);
        }

        public Encounter(int tick_id, List<CombatComponent> newcs)
        {
            this.tick_id = tick_id;
            cs = newcs;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="update"></param>
        public void update(CombatComponent update)
        {
            cs.Add(update);
            //TODO: reset timeout?
        }


        public void orderByTick()
        {
            cs.OrderBy(x => x.tick_id); //TODO evtl descending?
        }

        public bool hasTimeout(int currenttick)
        {
            if(tick_id+TTD >= currenttick)
                return true;

            return false;
        }
    }
}
