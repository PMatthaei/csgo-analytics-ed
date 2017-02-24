using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_Analytics.src.data.exceptions
{
    class InsufficientPlayersException : Exception
    {
        /// <summary>
        /// Constructor used with a message.
        /// </summary>
        /// <param name="message">String message of exception.</param>
        public InsufficientPlayersException()
        : base("Insufficient amount of players ( < 10) to beginn encounter detection. Maybe a player did not connect at all or a bot was choosen instead.")
        {
        }
    }
}
