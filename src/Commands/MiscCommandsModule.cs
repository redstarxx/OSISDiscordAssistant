using System;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus;
using Microsoft.Extensions.PlatformAbstractions;
using Npgsql;
using OSISDiscordAssistant.Utilities;
using OSISDiscordAssistant.Services;
using OSISDiscordAssistant.Models;

namespace OSISDiscordAssistant.Commands
{
    class MiscCommandsModule : BaseCommandModule
    {
        private readonly EventContext _eventsContext;

        public MiscCommandsModule(EventContext eventContext)
        {
            _eventsContext = eventContext;
        }

        [Command("about")]
        public async Task AboutAsync(CommandContext ctx)
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
                Description = string.Concat($"OSIS Discord Assistant is a bot created by RedStar#9271 (<@!322693857760509952>). " +
                $"This bot is developed to assist the members of OSIS Sekolah Djuwita Batam to carry out its tasks in terms " +
                $"of event planning, automated deadline reminders and server administration. The source code is available {Formatter.MaskedUrl("here.", new Uri("https://github.com/redstarxx/OSISDiscordAssistant"))}" +
                $"\n\n{Formatter.MaskedUrl("Add me to your server!", new Uri("https://discord.com/api/oauth2/authorize?client_id=382165423979888653&permissions=8&scope=bot"))}"),
                Color = DiscordColor.MidnightBlue,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"This shard is currently servicing {ctx.Client.Guilds.Count} servers. | Shard ID: {ctx.Client.ShardId}."
                }
            };

            embedBuilder.AddField("Bot Version", Formatter.Bold(ccv), true)
                .AddField("DSharpPlus Version", Formatter.Bold(dsv), true)
                .AddField(".NET Core Version", Formatter.Bold(ncv), true)
                .AddField("PostgreSQL Version", Formatter.Bold(psqlv), true)
                .AddField("Humanizr Version", Formatter.Bold(hmnzv), true)
                .AddField("Entity Framework Core Version", Formatter.Bold(efcv), true);

            await ctx.Channel.SendMessageAsync(embed: embedBuilder);
        }

        [Command("stats")]
        public async Task UptimeAsync(CommandContext ctx)
        {
            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
            {
            };

            Process currentProcess = Process.GetCurrentProcess();
            TimeSpan runtime = DateTime.Now - Process.GetCurrentProcess().StartTime;

            Stopwatch stopwatch = new();
            stopwatch.Start();
            _eventsContext.Events.FirstOrDefault(x => x.Id == 0);
            stopwatch.Stop();

            embedBuilder.WithTitle("Statistics for OSIS Discord Assistant")
                        .WithColor(DiscordColor.DarkBlue)
                        .AddField("Discord WebSocket Latency", $"{ctx.Client.Ping} ms", true)
                        .AddField("Database Latency", $"{stopwatch.ElapsedMilliseconds} ms", true)
                        .AddField("Total Guilds", ctx.Client.Guilds.Count().ToString(), true)
                        .AddField("Total Members", ctx.Client.Guilds.Values.SelectMany(x => x.Members.Keys).Count().ToString(), true)
                        .AddField("Shards", ctx.Client.ShardCount.ToString(), true)
                        .AddField("Uptime", $"{runtime.Days} days, {runtime.Hours} hours, {runtime.Minutes} minutes, {runtime.Seconds} seconds (since {Formatter.Timestamp(currentProcess.StartTime, TimestampFormat.LongDateTime)})")
                        .AddField("Memory Usage", $"{GC.GetTotalMemory(true) / 1024 / 1024:n2} MB", true);

            await ctx.Channel.SendMessageAsync(embed: embedBuilder);
        }

        [Command("ping")]
        public async Task PingAsync(CommandContext ctx)
        {
            await ctx.RespondAsync(string.Concat("\u200b", DiscordEmoji.FromName(ctx.Client, ":ping_pong:"),
                " WebSocket latency: ", ctx.Client.Ping.ToString("#,##0"), "ms."));
        }

        [Command("avatar")]
        public async Task AvatarAsync(CommandContext ctx, DiscordMember member = null)
        {
            DiscordMember memberProfilePicture = member ?? ctx.Member;

            var profileImageLink = memberProfilePicture.GetAvatarUrl(ImageFormat.Png);
            await ctx.Channel.SendMessageAsync(profileImageLink);
        }

        [Command("afk")]
        public async Task SetAFKStatusAsync(CommandContext ctx)
        {
            if (!ctx.Member.Nickname.Contains("[AFK]"))
            {
                await ctx.Member.ModifyAsync(x => x.Nickname = $"[AFK] {ctx.Member.Nickname}");

                await ctx.RespondAsync("You are now away from keyboard!");
            }

            else
            {
                await ctx.Member.ModifyAsync(x => x.Nickname = ctx.Member.Nickname.Replace("[AFK] ", ""));

                await ctx.RespondAsync("You are no longer away from keyboard!");
            }
        }

        [Command("slap")]
        public async Task SlapAsync(CommandContext ctx, DiscordMember member)
        {
            await ctx.Channel.SendMessageAsync("https://tenor.com/view/nope-stupid-slap-in-the-face-phone-gif-15151334");

            await ctx.Channel.SendMessageAsync($"{member.Mention} has been slapped by {ctx.Member.Mention}!");
        }

        [Command("flip")]
        public async Task FlipAsync(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync($"{ctx.Member.DisplayName} places the coin on their thumb and flips it. You meticulously watch the coin spinning as it descends from the sky...");
            await ctx.TriggerTypingAsync();

            Random rng = new Random();

            int num = rng.Next(1000);

            string side = null;
            if (num % 2 == 0)
            {
                side = "tails";
            }

            else
            {
                side = "head";
            }

            Thread.Sleep(TimeSpan.FromSeconds(2));

            await ctx.Channel.SendMessageAsync($"It lands on {Formatter.Bold(side)}!");
        }

        [Command("snipe")]
        public async Task SnipeAsync(CommandContext ctx)
        {
            if (SharedData.DeletedMessages.ContainsKey(ctx.Channel.Id))
            {
                var message = SharedData.DeletedMessages[ctx.Channel.Id];

                var messageContent = message.Content;

                if (messageContent.Length > 500)
                {
                    messageContent = messageContent.Substring(0, 500) + "...";
                }

                DiscordEmbedBuilder snipeEmbed = new DiscordEmbedBuilder()
                {
                    Author = new DiscordEmbedBuilder.EmbedAuthor
                    {
                        Name = $"{message.Author.Username}#{message.Author.Discriminator}",
                        IconUrl = message.Author.AvatarUrl
                    },
                    Timestamp = message.CreationTimestamp
                };

                if (!string.IsNullOrEmpty(message.Content))
                {
                    snipeEmbed.WithDescription(message.Content);
                }

                await ctx.RespondAsync(embed: snipeEmbed.Build());

                return;
            }

            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} No message to snipe!");
        }

        [Command("snipeedit")]
        public async Task SnipeEditAsync(CommandContext ctx)
        {
            if (SharedData.EditedMessages.ContainsKey(ctx.Channel.Id))
            {
                var message = SharedData.EditedMessages[ctx.Channel.Id];

                var messageContent = message.Content;

                if (messageContent.Length > 500)
                {
                    messageContent = messageContent.Substring(0, 500) + "...";
                }

                DiscordEmbedBuilder snipeEmbed = new DiscordEmbedBuilder()
                {
                    Author = new DiscordEmbedBuilder.EmbedAuthor
                    {
                        Name = $"{message.Author.Username}#{message.Author.Discriminator}",
                        IconUrl = message.Author.AvatarUrl
                    },
                    Timestamp = message.EditedTimestamp
                };

                if (!string.IsNullOrEmpty(message.Content))
                {
                    snipeEmbed.WithDescription(message.Content);
                }

                await ctx.RespondAsync(embed: snipeEmbed.Build());

                return;
            }

            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} No message to snipe!");
        }

        [Command("myinfo")]
        public async Task MyInfoAsync(CommandContext ctx)
        {
            var embedBuilder = new DiscordEmbedBuilder
            {
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    Name = $"{ctx.Member.DisplayName}",
                    IconUrl = ctx.Member.AvatarUrl
                },
                Title = "Member Information",
                Description = $"Halo, {ctx.Member.Mention}! Informasi akun anda adalah sebagai berikut:",
                Timestamp = DateTime.Now,
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

            await ctx.Channel.SendMessageAsync(embed: embedBuilder);
        }

        [Command("prefix")]
        public async Task PrefixAsync(CommandContext ctx)
        {          
            await ctx.RespondAsync($"{Formatter.Bold("[PREFIX]")} My prefixes are {ClientUtilities.GetPrefixList()}.");
        }
    }
}
