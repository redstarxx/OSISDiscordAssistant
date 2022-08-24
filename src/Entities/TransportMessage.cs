using System;
using DSharpPlus.Entities;

namespace OSISDiscordAssistant.Entities
{
    public class TransportMessage
    {
        public DateTime DateTime { get; set; }

        public DiscordMessage Message { get; set; }
    }
}
