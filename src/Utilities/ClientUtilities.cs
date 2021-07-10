using System;
using System.Collections.Generic;
using System.Text;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;

namespace discordbot
{
    public class ClientUtilities
    {
        /// <summary>
        /// Checks whether the targeted userID has either of the following two roles.
        /// 823950698189553724 as Service Administrator role ID and 814450825702801421 as Inti OSIS role ID.
        /// If true, errorReason is sent as reply.
        /// </summary>
        /// <param name="userID">User to check for.</param>
        /// <param name="ctx">CommandContext belonging to the executing command.</param>
        /// <returns>False if has none of the roles above.</returns>
        public static async Task<bool> CheckAdminPermissions(ulong userID, CommandContext ctx)
        {
            DiscordMember member = await ctx.Guild.GetMemberAsync(userID);
            var roleList = string.Join(", ", member.Roles);
            if (!roleList.Contains("823950698189553724"))
            {
                if (!roleList.Contains("814450825702801421"))
                {
                    string errorReason = "**[ERROR]** This command is restricted to administrators only.";
                    await ctx.Channel.SendMessageAsync(errorReason).ConfigureAwait(false);
                    return false;
                }

                else if (roleList.Contains("814450825702801421"))
                {
                    return true;
                }
            }

            else if (roleList.Contains("823950698189553724"))
            {
                return true;
            }

            return true;
        }

        /// <summary>
        /// Checks whether the member has the Service Administrator role.
        /// </summary>
        /// <param name="userID">User to check for.</param>
        /// <param name="ctx">CommandContext belonging to the executing command.</param>
        /// <returns>False if has none of the roles above.</returns>
        public static bool CheckServiceAdminRole(CommandContext ctx)
        {
            var roleList = string.Join(", ", ctx.Member.Roles);
            if (!roleList.Contains("Service Administrator"))
            {
                return false;
            }

            else if (roleList.Contains("Service Administrator"))
            {
                return true;
            }

            return true;
        }

        /// <summary>
        /// Checks whether the command user is using the command towards themself.
        /// If true, errorReason is sent as reply.
        /// </summary>
        /// <param name="member">Targeted member for the command execution.</param>
        /// <param name="ctx">CommandContext belonging to the executing command.</param>
        /// <returns>True if user is equals targeted user.</returns>
        public static async Task<bool> CheckSelfTargeting(DiscordMember member, CommandContext ctx)
        {
            if (ctx.User.Id == member.Id)
            {
                string errorReason = "**[ERROR]** You cannot use this command on yourself.";
                await ctx.Channel.SendMessageAsync(errorReason).ConfigureAwait(false);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Parses a shortened timespan into seconds. Example: "1d16h35m". 
        /// </summary>
        /// <param name="input">String to parse into seconds.</param>
        /// <returns></returns>
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
        /// <returns></returns>
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
    }
}
