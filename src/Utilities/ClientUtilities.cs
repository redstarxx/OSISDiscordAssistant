using System;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using OSISDiscordAssistant.Services;
using OSISDiscordAssistant.Enums;
using OSISDiscordAssistant.Models;

namespace OSISDiscordAssistant.Utilities
{
    public class ClientUtilities
    {
        /// <summary>
        /// Checks whether the respective CommandContext has either the Inti OSIS, Administrator or Service Administrator role.
        /// If false, error message is sent as reply.
        /// </summary>
        /// <returns>False if has none of the roles above.</returns>
        public static bool CheckAdminPermissions(CommandContext ctx)
        {
            // Role checks below.
            bool hasServiceAdminRole = CheckServiceAdminRole(ctx);

            bool hasAdminRole = ctx.Member.Roles.Any(x => x.Name == "Administrator");

            bool hasCoreCouncilRole = ctx.Member.Roles.Any(x => x.Name == "Inti OSIS");

            if (!hasServiceAdminRole)
            {
                if (!hasAdminRole)
                {
                    if (!hasCoreCouncilRole)
                    {
                        return false;
                    }

                    else
                    {
                        return true;
                    }
                }

                else
                {
                    return true;
                }
            }

            else
            {
                return true;
            }
        }

        /// <summary>
        /// Checks whether the member has the Service Administrator role.
        /// </summary>
        /// <returns>False if the Service Administrator role is not assigned.</returns>
        public static bool CheckServiceAdminRole(CommandContext ctx)
        {
            return ctx.Member.Roles.Any(x => x.Name == "Service Administrator");
        }

        /// <summary>
        /// Checks whether the command invoker has the OSIS role assigned or not.
        /// </summary>
        /// <returns>False if the OSIS role is not assigned.</returns>
        public static bool CheckAccessRole(CommandContext ctx)
        {
            return ctx.Member.Roles.Any(x => x.Name == "OSIS");
        }

        /// <summary>
        /// Checks whether the file name contains a file extension that is allowed.
        /// </summary>
        /// <param name="fileName">The file name to check.</param>
        /// <returns>True if the file name contains an allowed file extension.</returns>
        public static bool IsExtensionValid(string fileName)
        {
            string[] fileExtensions = 
            { 
                ".docx", ".docm", ".doc"
            };

            return fileExtensions.Any(x => fileName.Contains(x));
        }

        /// <summary>
        /// Parses a shortened timespan into seconds. Example: "1d16h35m". 
        /// </summary>
        /// <param name="input">String to parse into seconds.</param>
        /// <returns>TimeSpan in seconds.</returns>
        public static TimeSpan ParseToSeconds(string input)
        {
            var m = Regex.Match(input, @"^((?<days>\d+)d)?((?<hours>\d+)h)?((?<minutes>\d+)m)?((?<seconds>\d+)s)?$", RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.RightToLeft);

            int ds = m.Groups["days"].Success ? int.Parse(m.Groups["days"].Value) : 0;
            int hs = m.Groups["hours"].Success ? int.Parse(m.Groups["hours"].Value) : 0;
            int ms = m.Groups["minutes"].Success ? int.Parse(m.Groups["minutes"].Value) : 0;
            int ss = m.Groups["seconds"].Success ? int.Parse(m.Groups["seconds"].Value) : 0;

            TimeSpan timeSpan = TimeSpan.FromSeconds(ds * 86400 + hs * 60 * 60 + ms * 60 + ss);
            if (timeSpan.TotalSeconds is 0)
                throw new ArgumentException("Invalid time span string given.");

            return timeSpan;
        }

        /// <summary>
        /// Converts the bool value to the specified option.
        /// </summary>
        /// <param name="boolValue">The bool value.</param>
        /// <param name="convertOption">The result string option.</param>
        /// <returns>A string based on the choosen ConvertBoolOption enum.</returns>
        public static string ConvertBoolValue(bool boolValue, ConvertBoolOption convertOption)
        {
            if (convertOption is ConvertBoolOption.UpcomingOrDone)
            {
                switch (boolValue)
                {
                    case true:
                        return "Done.";
                    case false:
                        return "Upcoming.";
                }
            }

            else if (convertOption is ConvertBoolOption.YesOrNo)
            {
                switch (boolValue)
                {
                    case true:
                        return "Yes.";
                    case false:
                        return "No.";
                }
            }

            else if (convertOption is ConvertBoolOption.StoredOrNotStored)
            {
                switch (boolValue)
                {
                    case true:
                        return "Stored.";
                    case false:
                        return "Not stored.";
                }
            }

            return null;
        }

        /// <summary>
        /// Performs calculation of the given unix timestamp concerning when to send the event reminder.
        /// </summary>
        /// <param name="eventDateUnixTimestamp">The unix timestamp of the event date time.</param>
        /// <param name="backgroundServiceCalculation">Sets whether the calculation should be adjusted for EventsReminderService-like calculation needs.</param>
        /// <returns>An <see cref="Events" /> object which contains the calculated <see cref="Events.NextScheduledReminderUnixTimestamp" /> along with its respective data such as executed reminder level and expired status.</returns>
        public static Events CalculateEventReminderDate(long eventDateUnixTimestamp, bool backgroundServiceCalculation = false)
        {
            DateTime eventDateTime = ConvertUnixTimestampToDateTime(eventDateUnixTimestamp);
            TimeSpan timeSpan = eventDateTime - DateTime.Today;

            Events data = new Events();

            data.EventDateUnixTimestamp = eventDateUnixTimestamp;
            data.Expired = false;

            // Level 1, remind 30 days prior to the event.
            if (timeSpan.Days > 30 || timeSpan.Days == 30)
            {
                if (backgroundServiceCalculation is false)
                {
                    data.NextScheduledReminderUnixTimestamp = ConvertDateTimeToUnixTimestamp(eventDateTime.Subtract(TimeSpan.FromDays(30)));
                    data.ExecutedReminderLevel = 1;
                }

                else
                {
                    data.NextScheduledReminderUnixTimestamp = ConvertDateTimeToUnixTimestamp(eventDateTime.Subtract(TimeSpan.FromDays(14)));
                    data.ExecutedReminderLevel = 2;
                }
            }

            // Level 2, remind 14 days prior to the event.
            else if ((timeSpan.Days > 14 && timeSpan.Days < 30) || timeSpan.Days == 14)
            {
                if (backgroundServiceCalculation is false)
                {
                    data.NextScheduledReminderUnixTimestamp = ConvertDateTimeToUnixTimestamp(eventDateTime.Subtract(TimeSpan.FromDays(14)));
                    data.ExecutedReminderLevel = 2;
                }

                else
                {
                    data.NextScheduledReminderUnixTimestamp = ConvertDateTimeToUnixTimestamp(eventDateTime.Subtract(TimeSpan.FromDays(7)));
                    data.ExecutedReminderLevel = 3;
                }
            }

            // Level 3, remind 7 days prior to the event.
            else if ((timeSpan.Days > 7 && timeSpan.Days < 14) || timeSpan.Days == 7)
            {
                if (backgroundServiceCalculation is false)
                {
                    data.NextScheduledReminderUnixTimestamp = ConvertDateTimeToUnixTimestamp(eventDateTime.Subtract(TimeSpan.FromDays(7)));
                    data.ExecutedReminderLevel = 3;
                }

                else
                {
                    data.NextScheduledReminderUnixTimestamp = ConvertDateTimeToUnixTimestamp(eventDateTime.Subtract(TimeSpan.FromDays(1)));
                    data.ExecutedReminderLevel = 4;
                }
            }

            // Level 4, remind the day prior to the event.
            else if (timeSpan.Days < 7 && timeSpan.Days > 0)
            {
                if (backgroundServiceCalculation is false)
                {
                    data.NextScheduledReminderUnixTimestamp = ConvertDateTimeToUnixTimestamp(eventDateTime.Subtract(TimeSpan.FromDays(1)));
                    data.ExecutedReminderLevel = 4;
                }

                else
                {
                    data.NextScheduledReminderUnixTimestamp = 0;
                    data.ExecutedReminderLevel = 0;
                }
            }

            // If event is created at the day of the event or after the day of the event, do not remind.
            else if (timeSpan.Days == 0)
            {
                data.Expired = true;
                data.NextScheduledReminderUnixTimestamp = 0;
                data.ExecutedReminderLevel = 0;
            }

            else if (timeSpan.TotalDays < 0)
            {
                data.Expired = true;
                data.ExecutedReminderLevel = 0;
            }

            return data;
        }

        /// <summary>
        /// Gets the unix timestamp from the given <see cref="DateTime" /> object.
        /// </summary>
        /// <param name="dateTime">The <see cref="DateTime" /> object to calculate the unix timestamp from.</param>
        /// <returns>The unix timestamp, converted to UTC time.</returns>
        public static long ConvertDateTimeToUnixTimestamp(DateTime dateTime)
        {
            return (long)dateTime.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        /// <summary>
        /// Gets the <see cref="DateTime" /> from the specified unix timestamp.
        /// </summary>
        /// <param name="unixTimestamp">The unix timestamp to calculate the <see cref="DateTime" />.</param>
        /// <returns>A <see cref="DateTime" /> object from the specified unix timestamp, converted to local time.</returns>
        public static DateTime ConvertUnixTimestampToDateTime(long unixTimestamp)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixTimestamp).ToLocalTime();
        }

        /// <summary>
        /// Gets the current unix timestamp.
        /// </summary>
        /// <returns></returns>
        public static long GetCurrentUnixTimestamp()
        {
            return (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        /// <summary>
        /// An async version of reading the content of config.json which contains the connection string to the PostgreSQL database and deserializes it.
        /// </summary>
        /// <returns>The database connection string.</returns>
        public static async Task<string> GetConnectionStringAsync()
        {
            var json = string.Empty;
            using (var fileString = File.OpenRead("config.json"))
            using (var stringReader = new StreamReader(fileString, new UTF8Encoding(false)))
                json = await stringReader.ReadToEndAsync();

            var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);

            string connectionString = configJson.ConnectionString;
            return connectionString;
        }

        /// <summary>
        /// Reads the content of config.json which contains the connection string to the PostgreSQL database and deserializes it.
        /// </summary>
        /// <returns>The database connection string.</returns>
        public static string GetConnectionString()
        {
            var json = string.Empty;
            using (var fileString = File.OpenRead("config.json"))
            using (var stringReader = new StreamReader(fileString, new UTF8Encoding(false)))
                json = stringReader.ReadToEnd();

            var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);

            string connectionString = configJson.ConnectionString;
            return connectionString;
        }

        /// <summary>
        /// Loads the config.json file values for the bot to function properly.
        /// </summary>
        public static void LoadConfigurationValues()
        {
            var json = string.Empty;

            // Read the config.json file which contains the configuration values necessary for the bot to run properly.
            using (var fileString = File.OpenRead("config.json"))
            using (var stringReader = new StreamReader(fileString, new UTF8Encoding(false)))
                json = stringReader.ReadToEnd();

            var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);

            SharedData.Token = configJson.Token;

            SharedData.Prefixes = configJson.Prefix;

            SharedData.DbConnectionString = configJson.ConnectionString;

            SharedData.BotAdministratorId = configJson.BotAdministratorId;

            SharedData.MainGuildId = (ulong)configJson.MainGuildId;

            SharedData.StatusActivityType = configJson.StatusActivityType;

            SharedData.CustomStatusDisplay = configJson.CustomStatusDisplay;

            SharedData.EventChannelId = (ulong)configJson.EventChannelId;

            SharedData.ProposalChannelId = (ulong)configJson.ProposalChannelId;

            SharedData.VerificationRequestsProcessingChannelId = (ulong)configJson.VerificationRequestsProcessingChannelId;

            SharedData.MaxPendingVerificationWaitingDay = (int)configJson.MaxPendingVerificationWaitingDay;

            SharedData.VerificationInfoChannelId = (ulong)configJson.VerificationInfoChannelId;

            SharedData.RolesChannelId = (ulong)configJson.RolesChannelId;

            SharedData.ErrorChannelId = (ulong)configJson.ErrorChannelId;

            SharedData.AccessRoleId = (ulong)configJson.AccessRoleId;

            SharedData.MainGuildInviteLink = configJson.MainGuildInviteLink;

            JObject roleArray = JObject.Parse(json);

            // Read the MainGuildRoles array which contains the main guild assignable divisional roles.
            foreach (JObject result in roleArray["MainGuildRoles"])
            {                
                SharedData.AssignableRolesInfo roles = new SharedData.AssignableRolesInfo() 
                {
                    RoleId = (ulong)result["RoleId"],
                    RoleName = (string)result["RoleName"],
                    RoleEmoji = (string)result["RoleEmoji"]
                };

                SharedData.AvailableRoles.Add(roles);
            }
        }

        /// <summary>
        /// Gets the prefixes that are loaded from the config.json file.
        /// </summary>
        /// <returns>The list of prefixes formatted beautifully.</returns>
        public static string GetPrefixList()
        {
            int prefixCount = 0;
            int processedCount = 0;
            string prefixList = string.Empty;

            foreach (string prefix in SharedData.Prefixes)
            {
                prefixCount++;
            }

            foreach (string prefix in SharedData.Prefixes)
            {
                if (processedCount == 0)
                {
                    prefixList = $"{Formatter.InlineCode(prefix)}";
                    processedCount++;
                }

                else if (prefixCount - 1 == processedCount)
                {
                    prefixList = $"{prefixList} and {Formatter.InlineCode(prefix)}";
                    processedCount++;
                }

                else
                {
                    prefixList = $"{prefixList}, {Formatter.InlineCode(prefix)}";
                    processedCount++;
                }
            }

            return prefixList;
        }

        /// <summary>
        /// Gets the current version of the bot.
        /// </summary>
        /// <returns>The version number in major.minor.patch format.</returns>
        public static string GetBuildVersion()
        {
            return $"v{Assembly.GetExecutingAssembly().GetName().Version.ToString(3)}";
        }
    }
}
