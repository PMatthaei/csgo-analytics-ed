﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_Analytics.src.json.jsonobjects
{
    public class JSONGamestate
    {
        public JSONGamemeta meta { get; set; }
        public JSONMatch match { get; set; }
    }
}
