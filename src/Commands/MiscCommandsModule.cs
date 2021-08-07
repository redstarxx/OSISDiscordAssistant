using System;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.PlatformAbstractions;
using System.IO;
using Npgsql;
using discordbot;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace discordbot.Commands
{
    class MiscCommandsModule : BaseCommandModule
    {
        // Add 7 hours ahead because for some reason Linux doesn't pick the user preferred timezone.
        DateTime startTime = DateTime.UtcNow.AddHours(7);

        [Command("about")]
        public async Task BotInfo(CommandContext ctx)
        {
            var ccv = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

            var dsv = ctx.Client.VersionString;
            var ncv = PlatformServices.Default
                .Application
                .RuntimeFramework
                .Version
                .ToString(2);

            var cs = await ClientUtilities.GetConnectionStringAsync();
            await using var con = new NpgsqlConnection(cs);
            con.Open();

            var sql = "SHOW server_version";
            await using var cmd = new NpgsqlCommand(sql, con);
            var psqlv = cmd.ExecuteScalar().ToString();

            try
            {
                var a = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().Name == "System.Private.CoreLib");
                var pth = Path.GetDirectoryName(a.Location);
                pth = Path.Combine(pth, ".version");
                using (var fs = File.OpenRead(pth))
                using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                {
                    await sr.ReadLineAsync();
                    ncv = await sr.ReadLineAsync();
                }
            }
            catch { }

            FileVersionInfo humanizr = FileVersionInfo.GetVersionInfo(Environment.CurrentDirectory + @"/Humanizer.dll");
            string hmnzv = $"{humanizr.ProductMajorPart}.{humanizr.ProductMinorPart}.{humanizr.ProductBuildPart}";

            FileVersionInfo efcore = FileVersionInfo.GetVersionInfo(Environment.CurrentDirectory + @"/Microsoft.EntityFrameworkCore.dll");
            string efcv = $"{efcore.ProductMajorPart}.{efcore.ProductMinorPart}.{efcore.ProductBuildPart}";

            var embedBuilder = new DiscordEmbedBuilder
            {
                Title = "About OSIS Discord Assistant",
                Description =
                string.Concat($"OSIS Discord Assistant is a bot created by RedStar#9271 (<@!322693857760509952>). " +
                $"This bot is solely developed to assist the student council members of Sekolah Djuwita to carry out its tasks in terms " +
                $"of event planning, reminders and server administration. Feature extension beyond said purposes fully depends on the council's president " +
                $"in charge.\n\nThis bot is currently servicing {ctx.Client.Guilds.Count} guilds."),
                Color = DiscordColor.MidnightBlue
            };

            embedBuilder.AddField("Bot Version", Formatter.Bold(ccv), true)
                .AddField("DSharpPlus Version", Formatter.Bold(dsv), true)
                .AddField(".NET Core Version", Formatter.Bold(ncv), true)
                .AddField("PostgreSQL Version", Formatter.Bold(psqlv), true)
                .AddField("Humanizr Version", Formatter.Bold(hmnzv), true)
                .AddField("Entity Framework Core Version", Formatter.Bold(efcv), true);

            await ctx.Channel.SendMessageAsync(embed: embedBuilder);
        }
     
        [Command("uptime")]
        public async Task Uptime(CommandContext ctx)
        {
            TimeSpan runtimeOutput = DateTime.UtcNow.AddHours(7) - startTime;
            string formatResult = string.Format("Been up for: **{0} days, {1} hours, {2} minutes, {3} seconds** (since "
                + startTime.ToShortDateString() + " " + startTime.ToShortTimeString() + ")"
                , runtimeOutput.Days, runtimeOutput.Hours, runtimeOutput.Minutes, runtimeOutput.Seconds);

            await ctx.Channel.SendMessageAsync(formatResult).ConfigureAwait(false);
        }

        [Command("ping")]
        public async Task Ping(CommandContext ctx)
        {
            await ctx.RespondAsync(string.Concat("\u200b", DiscordEmoji.FromName(ctx.Client, ":ping_pong:"), 
                " WebSocket latency: ", ctx.Client.Ping.ToString("#,##0"), "ms."));
        }

        [Command("avatar")]
        public async Task AvatarLink(CommandContext ctx, DiscordMember member = null)
        {
            DiscordMember memberProfilePicture = member ?? ctx.Member;

            var profileImageLink = memberProfilePicture.GetAvatarUrl(ImageFormat.Png);
            await ctx.Channel.SendMessageAsync(profileImageLink).ConfigureAwait(false);
        }        

        [Command("slap")]
        public async Task Slap(CommandContext ctx, DiscordMember member)
        {
            await ctx.Channel.SendMessageAsync("https://tenor.com/view/nope-stupid-slap-in-the-face-phone-gif-15151334").ConfigureAwait(false);

            await ctx.Channel.SendMessageAsync($"{member.Mention} has been slapped by {ctx.Member.Mention}!").ConfigureAwait(false);
        }

        [Command("myinfo")]
        public async Task MyInfo(CommandContext ctx)
        {
            var embedBuilder = new DiscordEmbedBuilder
            {
                Title = "Member Information",
                Description = $"Halo, {ctx.Member.Mention}! Informasi akun anda adalah sebagai berikut:",
                Timestamp = ClientUtilities.GetWesternIndonesianDateTime(),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "OSIS Discord Assistant"
                },
                Color = DiscordColor.MidnightBlue
            };

            int roleCount = 0;
            string roleHeader = "Role";
            string roleList = "No roles";

            foreach (var roles in ctx.Member.Roles)
            {
                if (roleCount is 0)
                {
                    roleList = null;
                }

                string appendRoles = roleList + $" {roles.Mention}";
                roleList = appendRoles;

                roleCount++;
            }

            roleHeader = roleCount == 1 ? "Role" : "Roles";

            embedBuilder.AddField("Discord Tag", ctx.Member.Username + "#" + ctx.Member.Discriminator, true);
            embedBuilder.AddField("Discord User ID", ctx.Member.Id.ToString(), true);
            embedBuilder.AddField("Server Username", ctx.Member.DisplayName, true);
            embedBuilder.AddField(roleHeader, roleList, true);

            await ctx.Channel.SendMessageAsync(embed: embedBuilder).ConfigureAwait(false);
        }
    }
}
