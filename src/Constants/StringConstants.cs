namespace discordbot.Constants
{
    public static class StringConstants
    {
        /// <summary>
        /// The output template DateTime format for Serilog.
        /// </summary>
        public const string LogDateTimeFormat = "[{Timestamp:dd-MM-yyyy HH:mm:ss zzz} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";

        /// <summary>
        /// The guild ID of the main OSIS private server.
        /// </summary>
        public const ulong MainGuildId = 814445508583358494;

        /// <summary>
        /// The channel ID of the events reminder messages channel.
        /// </summary>
        public const ulong EventChannel = 857589614558314575;

        /// <summary>
        /// The channel ID of the proposal submissions reminder messages channel.
        /// </summary>
        public const ulong ProposalChannel = 857589664269729802;
    }
}
