using System;
using System.Collections.Generic;

namespace discordbot
{
    public class Counter
    {
        /// <summary>
        /// The ID of the respective row. Do not touch.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Poll counter number to get or set.
        /// </summary>
        public int PollCounter { get; set; }

        /// <summary>
        /// Verification request counter to get or set.
        /// </summary>
        public int VerifyCounter { get; set; }
    }
}
