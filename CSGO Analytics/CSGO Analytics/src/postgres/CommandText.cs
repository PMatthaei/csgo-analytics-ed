using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.data.gameobjects;

namespace CSGO_Analytics.src.postgres
{
    enum Events { };

    class CommandText
    {

        private static int MAXTICKS;
        /// <summary>
        /// Returns a range of ticks from startid tick (startid-rangeid / startid+rangeid)
        /// </summary>
        /// <param name="startid"></param>
        /// <param name="rangeid"></param>
        /// <returns></returns>
        public string getTicks()
        {
            return "";
        }

        /// <summary>
        /// Returns a range of ticks from startid tick (startid-rangeid / startid+rangeid)
        /// </summary>
        /// <param name="startid"></param>
        /// <param name="rangeid"></param>
        /// <returns></returns>
        public string getTickRangeCommand(int startid, int rangeid)
        {
            var min = startid - rangeid;
            if (min < 0)
                min = 0;
            var max = startid + rangeid;
            if (max > MAXTICKS)
                max = MAXTICKS;
            return "";
        }

        /// <summary>
        /// Get a certain tick by id
        /// </summary>
        /// <param name="tickid"></param>
        /// <returns></returns>
        public string getTickCommand(int tickid)
        {
            return "";
        }

        /// <summary>
        /// Get a certain round by id
        /// </summary>
        /// <param name="roundid"></param>
        /// <returns></returns>
        public string getRoundCommand(int roundid)
        {
            return "";
        }

        /// <summary>
        /// Get all events of type e
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public string getEventAllCommand(Events e)
        {
            return "";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public string getMatchMetaCommand(Events e)
        {
            return "";
        }

        /// <summary>
        /// Get all events of that player
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public string getPlayerAllCommand(Player p)
        {
            return "";
        }

        /// <summary>
        /// Get all players. if ids is not given get all else get a subset of players matching ids
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public string getPlayersCommand(int[] ids)
        {
            if (ids == null)
                return "";
            foreach(int id in ids)
            {

            }
            return "";
        }
    }
}
