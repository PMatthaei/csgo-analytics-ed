using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.data.gameobjects;
using CSGO_Analytics.src.json;

namespace CSGO_Analytics.src.encounterdetect.datasource
{


    public class TickStream : MemoryStream, IDisposable //TODO: is this useful? if so -> implement it
    {
        private StreamReader reader;

        public TickStream(Stream s)
        {
            s.CopyTo(this);
            this.Position = 0;
            reader = new StreamReader(this);
        }

        public Tick ReadTick()
        {

            string tickstring = reader.ReadLine(); //Correct position?
            dynamic deserializedTick = null;

            this.Position = this.Position + 1;
            var id = deserializedTick.tick_id;
            return new Tick(id, deserializedTick.gameevents);

        }

        public bool hasNextTick()
        {
            if (this.Position < this.Length)
                return true;

            reader.Close();
            return false;
        }
    }
}
