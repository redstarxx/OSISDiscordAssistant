using System;
using System.Collections.Generic;
using System.Text;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using System.Reflection;

namespace discordbot
{
    public class ClientUtilities
    {
        /// <summary>
        /// Checks whether the respective CommandContext has either the Inti OSIS, Administrator or Service Administrator role.
        /// If true, error message is sent as reply.
        /// </summary>
        /// <returns>False if has none of the roles above.</returns>
        public static async Task<bool> CheckAdminPermissions(CommandContext ctx)
        {
            // Administrator role ID.
            ulong adminRoleId = 814450538993156126;

            // Core council (Inti OSIS) role ID.
            ulong coreCouncilRoleId = 814450825702801421;

            // Role checks below.
            bool hasServiceAdminRole = CheckServiceAdminRole(ctx);

            bool hasAdminRole = ctx.Member.Roles.Any(x => x.Id == adminRoleId);

            bool hasCoreCouncilRole = ctx.Member.Roles.Any(x => x.Id == coreCouncilRoleId);

            if (!hasServiceAdminRole)
            {
                if (!hasAdminRole)
                {
                    if (!hasCoreCouncilRole)
                    {
                        string errorReason = $"{Formatter.Bold("[ERROR]")} This command is restricted to {Formatter.InlineCode("Inti OSIS")} and {Formatter.InlineCode("Administrator")} only.";
                        await ctx.Channel.SendMessageAsync(errorReason).ConfigureAwait(false);

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
            ulong serviceAdminRoleId = 823950698189553724;

            bool isServiceAdmin = false;

            isServiceAdmin = ctx.Member.Roles.Any(x => x.Id == serviceAdminRoleId);

            return isServiceAdmin;
        }

        /// <summary>
        /// Checks whether the command invoker has the OSIS role assigned or not.
        /// </summary>
        /// <returns>False if the OSIS role is not assigned.</returns>
        public static bool CheckAccessRole(CommandContext ctx)
        {
            ulong accessRoleId = 814450965565800498;

            bool hasAccess = false;

            hasAccess = ctx.Member.Roles.Any(x => x.Id == accessRoleId);

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
                await ctx.Channel.SendMessageAsync(errorReason).ConfigureAwait(false);
                return true;
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

            return TimeSpan.FromSeconds(ds * 86400 + hs * 60 * 60 + ms * 60 + ss);
        }

        /// <summary>
        /// Retrieves the role ID associated with the division name.
        /// </summary>
        /// <param name="divisionName">Name of the division to retrieve its role ID.</param>
        /// <returns>The role ID of the division name.</returns>
        public static ulong GetRoleID(string divisionName)
        {
            ulong roleID = 0;

            switch (divisionName.ToLowerInvariant())
            {
                case "inti":
                    roleID = 814450825702801421;
                    break;
                case "it":
                    roleID = 822423154864816138;
                    break;
                case "kesenian":
                    roleID = 822495032228708353;
                    break;
                case "kewirausahaan":
                    roleID = 822495096121327637;
                    break;
                case "olahraga":
                    roleID = 822495223694753852;
                    break;
                case "humas":
                    roleID = 823776027993833483;
                    break;
                case "agama":
                    roleID = 822495304300363796;
                    break;
                default:
                    break;
            }

            return roleID;
        }

        /// <summary>
        /// Converts the boolean property from the 'expired' row to be listed in the Events Manager listing.
        /// </summary>
        /// <param name="fromDb">The bool to convert from the 'expired' row.</param>
        /// <returns>If true, returns "Expired". If false, returns "Active".</returns>
        public static string ConvertStatusBoolean(bool fromDb)
        {
            string result = null;

            if (fromDb == true)
            {
                result = "Expired";
            }

            else
            {
                result = "Active";
            }

            return result;
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
                json = await stringReader.ReadToEndAsync().ConfigureAwait(false);

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
        /// Converts the local DateTime to Western Indonesian Time.
        /// </summary>
        /// <returns>GMT +7 DateTime</returns>
        public static DateTime GetWesternIndonesianDateTime()
        {
            DateTime currentTime = DateTime.Now;

            return currentTime;
        }

        /// <summary>
        /// Gets the current version of the bot.
        /// </summary>
        /// <returns>The version number in major.minor.patch format.</returns>
        public static string GetBuildVersion()
        {
            var ccv = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

            return ccv;
        }
    }
}
