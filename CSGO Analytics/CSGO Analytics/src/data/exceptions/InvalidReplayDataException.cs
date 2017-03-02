using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_Analytics.src.data.exceptions
{
    class InvalidReplayDataException : Exception
    {
        /// <summary>
        /// Constructor used with a message.
        /// </summary>
        /// <param name="message">String message of exception.</param>
        public InvalidReplayDataException(string invaliddata)
        : base("Data of the replay is invalid: "+ invaliddata)
        {
        }
    }
}
