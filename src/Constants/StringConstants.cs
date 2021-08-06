namespace discordbot.Constants
{
    public static class StringConstants
    {
        /// <summary>
        /// The output template DateTime format for Serilog.
        /// </summary>
        public const string LogDateTimeFormat = "[{Timestamp:dd-MM-yyyy HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";

        /// <summary>
        /// The guild ID of the main OSIS private server.
        /// </summary>
        public const ulong MainGuildId = 814445508583358494;
    }
}
