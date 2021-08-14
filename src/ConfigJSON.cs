using Newtonsoft.Json;

namespace OSISDiscordAssistant
{
    public struct ConfigJson
    {
        /// <summary>
        /// Gets the bot's token from the config file.
        /// </summary>
        [JsonProperty("token")]
        public string Token { get; private set; }

        /// <summary>
        /// Gets the commands prefix.
        /// </summary>
        [JsonProperty("prefix")]
        public string Prefix { get; private set; }

        /// <summary>
        /// Gets the PostgreSQL database connection string required for database operations.
        /// </summary>
        [JsonProperty("connectionstring")]
        public string ConnectionString { get; private set; }
    }
}
