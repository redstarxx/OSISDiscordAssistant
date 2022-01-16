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
using OSISDiscordAssistant.Constants;
using Humanizer;

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
            bool isServiceAdmin = false;

            isServiceAdmin = ctx.Member.Roles.Any(x => x.Name == "Service Administrator");

            return isServiceAdmin;
        }

        /// <summary>
        /// Checks whether the command invoker has the OSIS role assigned or not.
        /// </summary>
        /// <returns>False if the OSIS role is not assigned.</returns>
        public static bool CheckAccessRole(CommandContext ctx)
        {
            bool hasAccess = false;

            hasAccess = ctx.Member.Roles.Any(x => x.Name == "OSIS");

            return hasAccess;
        }

        /// <summary>
        /// Checks whether the command user is using the command towards themself.
        /// If true, errorReason is sent as reply.
        /// </summary>
        /// <param name="member">Targeted member for the command execution.</param>
        /// <param name="ctx">The respective CommandContext.</param>
        /// <returns>True if user is equals targeted user.</returns>
        public static async Task<bool> CheckSelfTargeting(DiscordMember member, CommandContext ctx)
        {
            if (ctx.User.Id == member.Id)
            {
                string errorReason = $"{Formatter.Bold("[ERROR]")} You cannot use this command on yourself.";
                await ctx.Channel.SendMessageAsync(errorReason);
                return true;
            }

            return false;
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

            foreach (string extension in fileExtensions)
            {
                if (fileName.Contains(extension))
                {
                    return true;
                }
            }

            return false;
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
        /// Retrieves the role ID associated with the division name.
        /// </summary>
        /// <param name="divisionName">Name of the division to retrieve its role ID.</param>
        /// <returns>The role ID of the division name.</returns>
        public static ulong? GetRoleID(DiscordGuild guild, string divisionName)
        {            
            if (divisionName.ToLowerInvariant() != "inti")
            {
                foreach (var role in SharedData.AvailableRoles.ToList())
                {
                    if (role.RoleName.ToLowerInvariant().Contains(divisionName.ToLowerInvariant()))
                    {
                        return role.RoleId;
                    }
                }
            }

            else
            {
                return guild.Roles.FirstOrDefault(x => x.Value.Name.ToLowerInvariant().Contains("inti")).Value.Id;
            }

            return null;
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
        /// Composes a reminder message.
        /// </summary>
        /// <param name="remindMessage">Something to remind (text, link, picture, whatever).</param>
        /// <param name="ctx">The CommandContext to attach from.</param>
        /// <param name="remindTarget">The target mention string.</param>
        /// <returns></returns>
        public static string CreateReminderMessage(string remindMessage, CommandContext ctx, string remindTarget)
        {
            if (ctx.Member.Mention == remindTarget)
            {
                return $"{DiscordEmoji.FromName(ctx.Client, ":alarm_clock:")} {ctx.Member.Mention}, you wanted to be reminded of the following: \n\n{remindMessage}";
            }

            else
            {
                return $"{DiscordEmoji.FromName(ctx.Client, ":alarm_clock:")} {remindTarget}, {ctx.Member.Mention} wanted to remind you of the following: \n\n{remindMessage}";
            }
        }

        /// <summary>
        /// Creates and fires a task which sends a reminder message after delaying from the specified timespan.
        /// </summary>
        /// <param name="remainingTime">The timespan object.</param>
        /// <param name="targetChannel">The DiscordChannel object that you want to send the reminder message to.</param>
        /// <param name="remindMessage">Something to remind (text, link, picture, whatever).</param>
        /// <param name="remindTarget">The target mention string.</param>
        public static void CreateReminderTask(TimeSpan remainingTime, DiscordChannel targetChannel, string remindMessage, CommandContext ctx, string remindTarget)
        {
            var reminderTask = new Task(async () =>
            {
                string reminderMessage = CreateReminderMessage(remindMessage, ctx, remindTarget);

                long fullDelays = remainingTime.Ticks / Constant.maxTimeSpanValue.Ticks;
                for (int i = 0; i < fullDelays; i++)
                {
                    await Task.Delay(Constant.maxTimeSpanValue);
                    remainingTime -= Constant.maxTimeSpanValue;
                }

                await Task.Delay(remainingTime);

                _ = targetChannel == ctx.Channel ? await ctx.RespondAsync(reminderMessage) : await targetChannel.SendMessageAsync(reminderMessage);
            });

            reminderTask.Start();
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

            SharedData.MainGuildId = (ulong)configJson.MainGuildId;

            SharedData.StatusActivityType = configJson.StatusActivityType;

            SharedData.CustomStatusDiplay = configJson.CustomStatusDiplay;

            SharedData.EventChannelId = (ulong)configJson.EventChannelId;

            SharedData.ProposalChannelId = (ulong)configJson.ProposalChannelId;

            SharedData.VerificationRequestsProcessingChannelId = (ulong)configJson.VerificationRequestsProcessingChannelId;

            SharedData.MaxPendingVerificationWaitingDay = (int)configJson.MaxPendingVerificationWaitingDay;

            SharedData.VerificationInfoChannelId = (ulong)configJson.VerificationInfoChannelId;

            SharedData.RolesChannelId = (ulong)configJson.RolesChannelId;

            SharedData.ErrorChannelId = (ulong)configJson.ErrorChannelId;

            SharedData.AccessRoleId = (ulong)configJson.AccessRoleId;

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
            return Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
        }
    }
}
