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

        /// <summary>
        /// The guild ID of the main OSIS private server.
        /// </summary>
        public static ulong MainGuildId;

        /// <summary>
        /// The channel ID of the events reminder messages channel.
        /// </summary>
        public static ulong EventChannelId;

        /// <summary>
        /// The channel ID of the proposal submissions reminder messages channel.
        /// </summary>
        public static ulong ProposalChannelId;

        /// <summary>
        /// The channel ID of the PRTask and ERTask exception logs channel.
        /// </summary>
        public static ulong ErrorChannelId;

        /// <summary>
        /// Get or set whether PRTask has been fired.
        /// </summary>
        public static bool IsProposalReminderInitialized = false;

        /// <summary>
        /// Get or set whether ERTask has been fired.
        /// </summary>
        public static bool IsEventReminderInitialized = false;

        /// <summary>
        /// Get or set whether status updater task has been fired.
        /// </summary>
        public static bool IsStatusUpdaterInitialized = false;
    }
}
