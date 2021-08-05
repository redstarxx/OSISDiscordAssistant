namespace discordbot.Constants
{
    public static class StringConstants
    {
        /// <summary>
        /// The output template DateTime format for Serilog.
        /// </summary>
        public const string LogDateTimeFormat = "[{Timestamp:dd-MM-yyyy HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";
    }
}
