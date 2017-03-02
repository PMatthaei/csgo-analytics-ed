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
    public class Encounter
    {
        /// <summary>
        /// Components which form this encounter
        /// </summary>
        public List<CombatComponent> cs;

        /// <summary>
        /// Tick in which this encounter arised
        /// </summary>
        public int tick_id;


        public Encounter(CombatComponent comp)
        {
            this.tick_id = comp.tick_id;
            cs = new List<CombatComponent>();
            AddComponent(comp);
        }

        public Encounter(int tick_id, List<CombatComponent> newcs)
        {
            this.tick_id = tick_id;
            cs = newcs.OrderBy(x => x.tick_id).ToList();
            cs.AsParallel().ForAll(x => x.parent = this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="update"></param>
        public void update(CombatComponent update)
        {
            AddComponent(update);
            cs = cs.OrderBy(x => x.tick_id).ToList();
        }

        public void AddComponent(CombatComponent comp)
        {
            cs.Add(comp);
            comp.parent= this;
        }

        public int getTickRange()
        {
            return cs.Max(c => c.tick_id) - cs.Min(c => c.tick_id);
        }

        public bool isDamageEncounter()
        {
            return cs.TrueForAll(component => component.links.TrueForAll( link => link.getLinkValue() == 0));
        }

        public override string ToString()
        {
            var s = "Encounter-TickID: " + tick_id + "\n";
            foreach (var c in cs)
            {
                s += c.ToString() + "\n";
            }
            return s;
        }

        override public bool Equals(object other)
        {
            var en = other as Encounter;
            if (en == null)
                return false;
            if (!(this.tick_id == en.tick_id))
                return false;

            var intersection = cs.Intersect(en.cs);

            if (intersection.Count() == cs.Count && intersection.Count() == en.cs.Count)
                return true;

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }


    }
}
