namespace OSISDiscordAssistant.Constants
{
    public static class StringConstants
    {
        /// <summary>
        /// The output template DateTime format for Serilog.
        /// </summary>
        public const string LogDateTimeFormat = "[{Timestamp:dd-MM-yyyy HH:mm:ss zzz} {Level:u3}] [{EventId}] {Message:lj}{NewLine}{Exception}";
    }
}
