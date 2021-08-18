using System.Collections.Concurrent;
using DSharpPlus.Entities;

namespace OSISDiscordAssistant.Services
{
    public class SharedData
    {
        /// <summary>
        /// The dictionary that stores deleted messages that has been previously cached.
        /// </summary>
        public static ConcurrentDictionary<ulong, DiscordMessage> DeletedMessages = new ConcurrentDictionary<ulong, DiscordMessage>();

        /// <summary>
        /// The dictionary that stores the original content of an edited message that has been previously cached.
        /// </summary>
        public static ConcurrentDictionary<ulong, DiscordMessage> EditedMessages = new ConcurrentDictionary<ulong, DiscordMessage>();
    }
}
