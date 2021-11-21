using System;

namespace OSISDiscordAssistant.Constants
{
    public static class Constant
    {
        /// <summary>
        /// The Serilog template format for writing logs into the console window.
        /// </summary>
        public const string LogConsoleFormat = "[{@t:dd-MM-yyyy HH:mm:ss zzz} {@l:u3}]{#if EventId is not null} [{EventId.Name}]{#end} {@m}\n{@x}";

        /// <summary>
        /// The Serilog template format for writing logs into a text file.
        /// </summary>
        public const string LogFileFormat = "[{Timestamp:dd-MM-yyyy HH:mm:ss zzz} {Level:u3}] [{EventId}] {Message:lj}{NewLine}{Exception}";

        /// <summary>
        /// Maximum value of a TimeSpan instance.
        /// </summary>
        public static readonly TimeSpan maxTimeSpanValue = TimeSpan.FromMilliseconds(int.MaxValue);
    }
}
