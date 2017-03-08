﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.math;

namespace CSGO_Analytics.src.data.gameobjects
{
    public class GameObject
    {
        /// <summary>
        /// Name of the object
        /// </summary>
        public string name;

        /// <summary>
        /// ID given by CS:GO and parsed by DemoInfo
        /// </summary>
        public int entityid;

        /// <summary>
        /// Current position of the gameobject
        /// </summary>
        public EDVector3D position;

        public int getID()
        {
            return entityid;
        }

        public EDVector3D getPosition()
        {
            return position;
        }
    }
}
