using System;

namespace OSISDiscordAssistant.Constants
{
    public static class Constant
    {
        /// <summary>
        /// The output template DateTime format for Serilog.
        /// </summary>
        public const string LogDateTimeFormat = "[{Timestamp:dd-MM-yyyy HH:mm:ss zzz} {Level:u3}] [{EventId}] {Message:lj}{NewLine}{Exception}";

        /// <summary>
        /// Maximum value of a TimeSpan instance.
        /// </summary>
        public static readonly TimeSpan maxTimeSpanValue = TimeSpan.FromMilliseconds(int.MaxValue);
    }
}
