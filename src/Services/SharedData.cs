using System.Collections.Concurrent;
using System.Collections.Generic;
using DSharpPlus.Entities;

namespace OSISDiscordAssistant.Services
{
    public class SharedData
    {
        /// <summary>
        /// Represents the main guild assignable divisional roles information.
        /// </summary>
        public struct AssignableRolesInfo
        {
            public ulong RoleId;
            public string RoleName;
            public string RoleEmoji;
        }

        /// <summary>
        /// The bot token provided by Discord.
        /// </summary>
        public static string Token;

        /// <summary>
        /// The PostgreSQL database connection string.
        /// </summary>
        public static string DbConnectionString;

        /// <summary>
        /// The prefixes used to execute a command.
        /// </summary>
        public static string[] Prefixes;

        /// <summary>
        /// The dictionary that stores deleted messages that has been previously cached.
        /// </summary>
        public static ConcurrentDictionary<ulong, DiscordMessage> DeletedMessages = new ConcurrentDictionary<ulong, DiscordMessage>();

        /// <summary>
        /// The dictionary that stores the original content of an edited message that has been previously cached.
        /// </summary>
        public static ConcurrentDictionary<ulong, DiscordMessage> EditedMessages = new ConcurrentDictionary<ulong, DiscordMessage>();

        /// <summary>
        /// The list that stores the list of the assignable divisional roles via a dropdown.
        /// </summary>
        public static List<AssignableRolesInfo> AvailableRoles = new List<AssignableRolesInfo>();

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
        /// The channel ID of the new member role verification request processing channel.
        /// </summary>
        public static ulong VerificationRequestsProcessingChannelId;

        /// <summary>
        /// The channel ID of the PRTask and ERTask exception logs channel.
        /// </summary>
        public static ulong ErrorChannelId;

        /// <summary>
        /// The ID of the role required to access the channels restricted to OSIS members in the main OSIS private server.
        /// </summary>
        public static ulong AccessRoleId;

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

        /// <summary>
        /// Get or set whether verification cleaning service task has been fired.
        /// </summary>
        public static bool IsVerificationCleanupTaskInitialized = false;
    }
}
