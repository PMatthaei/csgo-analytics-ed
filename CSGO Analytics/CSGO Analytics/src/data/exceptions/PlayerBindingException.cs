using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSGO_Analytics.src.data.exceptions
{
    class PlayerBindingException : Exception
    {
        /// <summary>
        /// Constructor used with a message.
        /// </summary>
        /// <param name="message">String message of exception.</param>
        public PlayerBindingException()
        : base("New Player was binded but cannot be allocated to an existing player.")
        {
        }

    }
}
