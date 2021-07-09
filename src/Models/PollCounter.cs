using System;
using System.Collections.Generic;

namespace discordbot
{
    public class PollCounter
    {
        /// <summary>
        /// The ID of the respective row. Do not touch.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Event name to get or set.
        /// </summary>
        public int Counter { get; set; }
    }
}
