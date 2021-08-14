using System;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using OSISDiscordAssistant.Commands;
using OSISDiscordAssistant.Attributes;
using OSISDiscordAssistant.Constants;
using OSISDiscordAssistant.Services;
using OSISDiscordAssistant.Utilities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Entities;
using Serilog;
using Serilog.Events;

namespace OSISDiscordAssistant
{
    public class Bot
    {
        public static DiscordShardedClient Client { get; private set; }

        public InteractivityExtension Interactivity { get; private set; }

        public IReadOnlyDictionary<int, CommandsNextExtension> Commands;

        public static EventId LogEvent { get; } = new EventId(1000, "BotClient");

        public static EventId ERTask { get; } = new EventId(2000, "ERTask");

        public static EventId PRTask { get; } = new EventId(3000, "PRTask");

        public static EventId StatusUpdater { get; } = new EventId(4000, "StatusUpdater");

        public async Task RunAsync()
        {
            // Displays the current version of the bot.
            Console.WriteLine($"DiscordBotOSIS v{ClientUtilities.GetBuildVersion()}");

            // Configures Serilog's Logger instance.
            Console.WriteLine("[1/9] Configuring logger instance...");
            Log.Logger = new LoggerConfiguration().WriteTo.Console(outputTemplate: StringConstants.LogDateTimeFormat)
                .WriteTo.File($@"{Environment.CurrentDirectory}/logs/clientlogs-.txt", LogEventLevel.Verbose, outputTemplate: StringConstants.LogDateTimeFormat,
                retainedFileCountLimit: null, rollingInterval: RollingInterval.Day, flushToDiskInterval: TimeSpan.FromMinutes(1)).CreateLogger();

            var serilogFactory = new LoggerFactory().AddSerilog();

            Console.WriteLine("[2/9] Reading config.json...");
            var json = string.Empty;
            using (var fileString = File.OpenRead("config.json"))
            using (var stringReader = new StreamReader(fileString, new UTF8Encoding(false)))
                json = await stringReader.ReadToEndAsync().ConfigureAwait(false);

            var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);

            Console.WriteLine("[3/9] Loading up client configuration...");
            var config = new DiscordConfiguration
            {
                Token = configJson.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                MinimumLogLevel = LogLevel.Information,
                Intents = DiscordIntents.All, 
                LoggerFactory = serilogFactory
            };

            Client = new DiscordShardedClient(config);

            Console.WriteLine("[4/9] Registering client event handlers...");
            Client.Ready += OnClientReady;
            Client.GuildDownloadCompleted += OnGuildDownloadCompleted;
            Client.GuildMemberAdded += OnGuildMemberAdded;
            Client.GuildMemberRemoved += OnGuildMemberRemoved;
            Client.GuildAvailable += OnGuildAvailable;
            Client.GuildCreated += OnGuildCreated;
            Client.GuildDeleted += OnGuildDeleted;
            Client.MessageCreated += OnMessageCreated;
            Client.MessageReactionAdded += OnMessageReactionAdded;
            Client.SocketErrored += OnSocketErrored;
            Client.Heartbeated += OnHeartbeated;
            Client.UnknownEvent += OnUnknownEvent;

            Console.WriteLine("[5/9] Loading up interactivity configuration...");
            await Client.UseInteractivityAsync(new InteractivityConfiguration
            {
                Timeout = TimeSpan.FromDays(7)
            });

            Console.WriteLine("[6/9] Loading up CommandsNext configuration...");
            var commandsConfig = new CommandsNextConfiguration
            {
                StringPrefixes = new String[] { configJson.Prefix },
                EnableMentionPrefix = true,
                EnableDms = false,
                EnableDefaultHelp = false
            };

            Commands = await Client.UseCommandsNextAsync(commandsConfig);

            Console.WriteLine("[7/9] Registering CommandsNext commands modules...");
            // Registers commands.
            foreach (var cmd in Commands.Values)
            {
                cmd.RegisterCommands<MiscCommandsModule>();
                cmd.RegisterCommands<VerificationCommandsModule>();
                cmd.RegisterCommands<ServerAdministrationCommandsModule>();
                cmd.RegisterCommands<ReminderCommandsModule>();
                cmd.RegisterCommands<EventCommandsModule>();
                cmd.RegisterCommands<HelpCommandsModule>();
                cmd.RegisterCommands<BotAdministrationCommands>();
                cmd.RegisterCommands<PollCommandsModule>();
                cmd.RegisterCommands<TagsCommandsModule>();
            }

            Console.WriteLine("[8/9] Registering CommandsNext event handlers...");
            // Registers event handlers.
            foreach (var hndlr in Commands.Values)
            {
                hndlr.CommandExecuted += CommandsNext_CommandExecuted;
                hndlr.CommandErrored += CommandsNext_CommandErrored;
            }

            // Tell that whoever is seeing this that the client is connecting to Discord's gateway.
            Console.WriteLine("[9/9] Initializing and connecting all shards...\n----------------------------------------");

            await Client.StartAsync();

            await Task.Delay(-1);
        }

        private Task OnClientReady(object sender, ReadyEventArgs e)
        {
            return Task.CompletedTask;
        }

        private Task OnGuildDownloadCompleted(object sender, GuildDownloadCompletedEventArgs e)
        {
            // Starts a new Discord status updater task to change the bot's display status every two minutes.
            BackgroundTasks.StartStatusUpdater();

            // Starts the events reminder task which queries the events table on a minute-by-minute basis.
            BackgroundTasks.StartEventReminders();

            // Starts the proposals reminder task which queries the events table on a daily basis.
            // Reminders are sent 30 days before or a week before the day of the event.
            BackgroundTasks.StartProposalReminders();

            Client.Logger.LogInformation(LogEvent, "Client is ready for tasking.", ClientUtilities.GetWesternIndonesianDateTime());

            return Task.CompletedTask;
        }                

        private Task OnMessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {
            if (e.Channel.IsPrivate && !e.Author.IsCurrent)
            {
                Client.Logger.LogInformation(LogEvent, $"User '{e.Author.Username}#{e.Author.Discriminator}' ({e.Author.Id}) sent \"{e.Message.Content}\" through Direct Messages ({e.Channel.Id})", ClientUtilities.GetWesternIndonesianDateTime());

                if (e.Message.Content.StartsWith("!"))
                {
                    string toSend = $"{Formatter.Bold("[ERROR]")} Sorry, you can only execute commands in a guild that the bot is a part of!";

                    e.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
                }
            }

            return Task.CompletedTask;
        }

        private Task OnMessageReactionAdded(object sender, MessageReactionAddEventArgs e)
        {
            Client.Logger.LogInformation(LogEvent,
                $"User '{e.User.Username}#{e.User.Discriminator}' ({e.User.Id}) " +
                $"added '{e.Emoji}' in #{e.Channel.Name} ({e.Channel.Id})",
                ClientUtilities.GetWesternIndonesianDateTime());

            return Task.CompletedTask;
        }

        private Task OnGuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs e)
        {
            sender.Logger.LogInformation(LogEvent, $"User added: {e.Member.Username}#{e.Member.Discriminator} ({e.Member.Id}) in {e.Guild.Name} ({e.Guild.Id})", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task OnGuildMemberRemoved(DiscordClient sender, GuildMemberRemoveEventArgs e)
        {
            sender.Logger.LogInformation(LogEvent, $"User removed: {e.Member.Username}#{e.Member.Discriminator} ({e.Member.Id}) in {e.Guild.Name} ({e.Guild.Id}).", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task OnGuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
        {
            sender.Logger.LogInformation(LogEvent, $"Guild available: {e.Guild.Name} ({e.Guild.Id})", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task OnGuildCreated(DiscordClient sender, GuildCreateEventArgs e)
        {
            sender.Logger.LogInformation(LogEvent, $"Guild added: {e.Guild.Name} ({e.Guild.Id})", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task OnGuildDeleted(DiscordClient sender, GuildDeleteEventArgs e)
        {
            sender.Logger.LogInformation(LogEvent, $"Guild removed: {e.Guild.Name} ({e.Guild.Id})", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task OnSocketErrored(DiscordClient sender, SocketErrorEventArgs e)
        {
            var ex = e.Exception;
            while (ex is AggregateException)
                ex = ex.InnerException;

            sender.Logger.LogCritical(LogEvent, $"Socket threw an exception {ex.GetType()}: {ex.Message}", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task OnHeartbeated(DiscordClient sender, HeartbeatEventArgs e)
        {
            sender.Logger.LogInformation(LogEvent, $"Received heartbeat ACK: {e.Ping} ms.", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task OnUnknownEvent(DiscordClient sender, UnknownEventArgs e)
        {
            e.Handled = true;

            sender.Logger.LogWarning(LogEvent, $"Received unknown event {e.EventName}, payload:\n{e.Json}", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task CommandsNext_CommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e)
        {
            e.Context.Client.Logger.LogInformation(LogEvent,
                $"User '{e.Context.User.Username}#{e.Context.User.Discriminator}' ({e.Context.User.Id}) " +
                $"executed '{e.Command.QualifiedName}' in #{e.Context.Channel.Name} ({e.Context.Channel.Id}) guild '{e.Context.Guild.Name}' ({e.Context.Guild.Id})",
                ClientUtilities.GetWesternIndonesianDateTime());

            return Task.CompletedTask;
        }

        private async Task<Task> CommandsNext_CommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            var failedChecks = ((ChecksFailedException)e.Exception).FailedChecks;
            foreach (var failedCheck in failedChecks)
            {
                if (failedCheck is RequireMainGuild)
                {
                    await e.Context.RespondAsync($"{Formatter.Bold("[ERROR]")} This command is only usable in the OSIS Sekolah Djuwita Batam Discord server!\nInvite Link: https://discord.gg/WC7FRsxFwb");
                }

                else if (failedCheck is RequireAdminRole)
                {
                    await e.Context.RespondAsync($"{Formatter.Bold("[ERROR]")} This command is restricted to members with administrator permissions only.");
                }

                else if (failedCheck is RequireAccessRole)
                {
                    await e.Context.RespondAsync($"{Formatter.Bold("[ERROR]")} This command is restricted to members with the {Formatter.InlineCode("OSIS")} role only.");
                }

                else if (failedCheck is RequireServiceAdminRole)
                {
                    await e.Context.RespondAsync($"{Formatter.Bold("[ERROR]")} This command is restricted to members with the {Formatter.InlineCode("Service Administrator")} role only.");
                }

                else if (failedCheck is RequireChannel)
                {
                    DiscordChannel requiredChannel = await e.Context.Client.GetChannelAsync(RequireChannel.Channel);

                    await e.Context.RespondAsync($"{Formatter.Bold("[ERROR]")} This command is only usable in {requiredChannel.Mention}!");
                }
            }

            e.Context.Client.Logger.LogError(LogEvent,
                $"User '{e.Context.User.Username}#{e.Context.User.Discriminator}' ({e.Context.User.Id}) tried to execute '{e.Command?.QualifiedName ?? "<unknown command>"}' "
                + $"in #{e.Context.Channel.Name} ({e.Context.Channel.Id})  guild '{e.Context.Guild.Name}' ({e.Context.Guild.Id}) and failed with {e.Exception.GetType()}: {e.Exception.Message}", ClientUtilities.GetWesternIndonesianDateTime());

            return Task.CompletedTask;
        }
    }
}
