﻿using System;
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
    class MiscCommands : BaseCommandModule
    {
        DateTime startTime = DateTime.Now;

        [Command("about")]
        public async Task BotInfo(CommandContext ctx)
        {
            var ccv = Assembly.GetExecutingAssembly().GetName().Version.ToString();

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
                $"in charge.")
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
            TimeSpan runtimeOutput = DateTime.Now - startTime;
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
        public async Task AvatarLink(CommandContext ctx, DiscordMember member)
        {
            var profileImageLink = member.GetAvatarUrl(ImageFormat.Png);
            await ctx.Channel.SendMessageAsync(profileImageLink).ConfigureAwait(false);
        }

        [Command("datetime")]
        public async Task CheckDate(CommandContext ctx)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            using (var db = new EventContext())
            {
                foreach (var row in db.Events)
                {
                    var cultureInfo = new CultureInfo(row.EventDateCultureInfo);
                    DateTime currentTime = DateTime.Now;
                    string combineCurrentDateTime = $"{currentTime.ToShortDateString()} {currentTime.ToShortTimeString()}";

                    DateTime toConvert = DateTime.Parse(row.EventDate, cultureInfo);
                    string combineEventDateTime = $"{toConvert.ToShortDateString()} {toConvert.ToShortTimeString()}";

                    await ctx.Channel.SendMessageAsync($"{combineCurrentDateTime} - {combineEventDateTime}").ConfigureAwait(false);
                }

                stopwatch.Stop();
                await ctx.RespondAsync($"It took {stopwatch.ElapsedMilliseconds} ms.").ConfigureAwait(false);
            }
        }

        [Command("slap")]
        public async Task Slap(CommandContext ctx, DiscordMember member)
        {
            await ctx.Channel.SendMessageAsync($"{member.Mention} has been slapped by {ctx.Member.Mention}!");
        }

        [Command("myinfo")]
        public async Task MyInfo(CommandContext ctx)
        {
            var embedBuilder = new DiscordEmbedBuilder
            {
                Title = "Member Information",
                Description = $"Halo, {ctx.Member.Mention}! Informasi akun anda adalah sebagai berikut:",
                Timestamp = DateTime.Now.AddHours(7),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "OSIS Discord Assistant"
                }
            };

            embedBuilder.AddField("Discord Tag", ctx.Member.Username + "#" + ctx.Member.Discriminator, true);
            embedBuilder.AddField("Discord User ID", ctx.Member.Id.ToString(), true);
            embedBuilder.AddField("Server Username", ctx.Member.DisplayName, true);

            await ctx.Channel.SendMessageAsync(embed: embedBuilder).ConfigureAwait(false);
        }
    }
}
