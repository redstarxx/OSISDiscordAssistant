using Newtonsoft.Json;

namespace OSISDiscordAssistant
{
    public struct ConfigJson
    {
        /// <summary>
        /// Gets the bot's token from the config file.
        /// </summary>
        [JsonProperty("Token")]
        public string Token { get; private set; }

        /// <summary>
        /// Gets the commands prefix.
        /// </summary>
        [JsonProperty("Prefix")]
        public string[] Prefix { get; private set; }

        /// <summary>
        /// Gets the PostgreSQL database connection string required for database operations.
        /// </summary>
        [JsonProperty("DbConnectionString")]
        public string ConnectionString { get; private set; }

        /// <summary>
        /// The guild ID of the main OSIS private server.
        /// </summary>
        [JsonProperty("MainGuildId")]
        public ulong? MainGuildId { get; private set; }

        /// <summary>
        /// The type of activity type to be set as the bot's status (ex: Watching, Streaming, Listening to).
        /// </summary>
        [JsonProperty("StatusActivityType")]
        public int StatusActivityType { get; private set; }

        /// <summary>
        /// The list of custom statuses to be set as the custom status of the bot on a two minute basis.
        /// </summary>
        [JsonProperty("CustomStatusDisplay")]
        public string[] CustomStatusDiplay { get; private set; }

        /// <summary>
        /// The channel ID of the events reminder messages channel.
        /// </summary>
        [JsonProperty("EventChannelId")]
        public ulong? EventChannelId { get; private set; }

        /// <summary>
        /// The channel ID of the proposal submissions reminder messages channel.
        /// </summary>
        [JsonProperty("ProposalChannelId")]
        public ulong? ProposalChannelId { get; private set; }

        /// <summary>
        /// The channel ID of the new member role verification request processing channel.
        /// </summary>
        [JsonProperty("VerificationRequestsProcessingChannelId")]
        public ulong? VerificationRequestsProcessingChannelId { get; private set; }

        /// <summary>
        /// The number of days a verification request stays valid.
        /// </summary>
        [JsonProperty("MaxPendingVerificationWaitingDay")]
        public int? MaxPendingVerificationWaitingDay { get; private set; }

        /// <summary>
        /// The channel ID of the channel containing the divisional roles dropdown button.
        /// </summary>
        [JsonProperty("RolesChannelId")]
        public ulong? RolesChannelId { get; private set; }

        /// <summary>
        /// The channel ID of the PRTask and ERTask exception logs channel.
        /// </summary>
        [JsonProperty("ErrorChannelId")]
        public ulong? ErrorChannelId { get; private set; }

        /// <summary>
        /// The ID of the role required to access the channels restricted to OSIS members in the main OSIS private server.
        /// </summary>
        [JsonProperty("AccessRoleId")]
        public ulong? AccessRoleId { get; private set; }
    }
}
