﻿using System;
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
using Serilog.Templates;

namespace OSISDiscordAssistant
{
    public class Bot
    {
        public static DiscordShardedClient Client { get; private set; }

        public InteractivityExtension Interactivity { get; private set; }

        public IReadOnlyDictionary<int, CommandsNextExtension> Commands;

        public async Task RunAsync()
        {
            // Displays the current version of the bot.
            Console.WriteLine($"OSISDiscordAssistant v{ClientUtilities.GetBuildVersion()}");

            // Configures Serilog's Logger instance.
            Console.WriteLine("[1/9] Configuring logger instance...");
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(new ExpressionTemplate(Constant.LogConsoleFormat))
                .WriteTo.File($@"{Environment.CurrentDirectory}/logs/clientlogs-.txt", LogEventLevel.Verbose, outputTemplate: Constant.LogFileFormat,
                retainedFileCountLimit: null, rollingInterval: RollingInterval.Day, flushToDiskInterval: TimeSpan.FromMinutes(1)).CreateLogger();

            var serilogFactory = new LoggerFactory().AddSerilog();

            Console.WriteLine("[2/9] Reading and loading config.json...");

            ClientUtilities.LoadConfigurationValues();

            Console.WriteLine("[3/9] Loading up client configuration...");
            var config = new DiscordConfiguration
            {
                Token = SharedData.Token,
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
            Client.MessageUpdated += OnMessageUpdated;
            Client.MessageDeleted += OnMessageDeleted;
            Client.MessageReactionAdded += OnMessageReactionAdded;
            Client.MessageReactionRemoved += OnMessageReactionRemoved;
            Client.ComponentInteractionCreated += OnComponentInteractionCreated;
            Client.SocketErrored += OnSocketErrored;
            Client.ClientErrored += OnClientErrored;
            Client.Heartbeated += OnHeartbeated;
            Client.UnknownEvent += OnUnknownEvent;

            Console.WriteLine("[5/9] Loading up interactivity configuration...");
            await Client.UseInteractivityAsync(new InteractivityConfiguration
            {
                Timeout = TimeSpan.FromDays(7),
                AckPaginationButtons = true,
            });

            Console.WriteLine("[6/9] Loading up CommandsNext configuration...");
            var commandsConfig = new CommandsNextConfiguration
            {
                StringPrefixes = SharedData.Prefixes,
                EnableMentionPrefix = true,
                EnableDms = false,
                EnableDefaultHelp = false,
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
            // Fires a new Discord status updater task to change the bot's display status every two minutes.
            BackgroundTasks.StartStatusUpdater();

            // Fires the events reminder task which queries the events table on a minute-by-minute basis.
            // The code explains for itself.
            BackgroundTasks.StartEventReminders();

            // Fires the proposals reminder task which queries the events table on a minute-by-minute basis.
            // Reminders are sent 30 days before or a week before the day of the event.
            BackgroundTasks.StartProposalReminders();

            // Fires the verification cleanup task which removes & marks a verification request as expired that has not been processed for 7 days.
            BackgroundTasks.StartVerificationCleanupTask();

            BackgroundTasks.StartHeartbeatMonitoringTask();

            Client.Logger.LogInformation(EventIds.Core, "Client is ready for tasking.", DateTime.Now);

            return Task.CompletedTask;
        }                

        private Task OnMessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {
            _ = Task.Run(async () =>
           {
               if (e.Channel.IsPrivate && !e.Author.IsCurrent)
               {
                   Client.Logger.LogInformation(EventIds.EventHandler, $"User '{e.Author.Username}#{e.Author.Discriminator}' ({e.Author.Id}) sent \"{e.Message.Content}\" through Direct Messages ({e.Channel.Id})", DateTime.Now);

                   if (e.Message.Content.StartsWith("!"))
                   {
                       string toSend = $"{Formatter.Bold("[ERROR]")} Sorry, you can only execute commands in a guild that the bot is a part of!";

                       await e.Channel.SendMessageAsync(toSend);
                   }
               }
           });

            return Task.CompletedTask;
        }

        private Task OnMessageUpdated(DiscordClient sender, MessageUpdateEventArgs e)
        {
            sender.Logger.LogInformation(EventIds.EventHandler,
                $"User '{e.Message.Author.Username}#{e.Message.Author.Discriminator}' ({e.Message.Author.Id}) " +
                $"updated message ({e.Message.Id}) in #{e.Channel.Name} ({e.Channel.Id}) guild '{e.Guild.Name}' ({e.Guild.Id}).",
                DateTime.Now);

            if (e.MessageBefore is null)
            {
                sender.Logger.LogInformation(EventIds.Core, $"Message ({e.Message.Id}) is not cached. Skipped storing previous message content.");

                return Task.CompletedTask;
            }

            if (!string.IsNullOrEmpty(e.MessageBefore.Content) || e.Message.Embeds.Count > 0)
            {
                if (SharedData.EditedMessages.ContainsKey(e.Channel.Id))
                {
                    SharedData.EditedMessages[e.Channel.Id] = e.MessageBefore;
                }

                else
                {
                    SharedData.EditedMessages.TryAdd(e.Channel.Id, e.MessageBefore);
                }
            }

            return Task.CompletedTask;
        }

        private Task OnMessageDeleted(DiscordClient sender, MessageDeleteEventArgs e)
        {
            try
            {
                sender.Logger.LogInformation(EventIds.EventHandler,
                    $"User '{e.Message.Author.Username}#{e.Message.Author.Discriminator}' ({e.Message.Author.Id}) " +
                    $"deleted message ({e.Message.Id}) in #{e.Channel.Name} ({e.Channel.Id}) guild '{e.Guild.Name}' ({e.Guild.Id}).",
                    DateTime.Now);
            }

            catch
            {
                sender.Logger.LogInformation(EventIds.Core, $"Message ({e.Message.Id}) was not cached. Skipped logging message deleted event.");

                return Task.CompletedTask;
            }

            if (!string.IsNullOrEmpty(e.Message.Content) || e.Message.Embeds.Count > 0)
            {
                if (SharedData.DeletedMessages.ContainsKey(e.Channel.Id))
                {
                    SharedData.DeletedMessages[e.Channel.Id] = e.Message;
                }

                else
                {
                    SharedData.DeletedMessages.TryAdd(e.Channel.Id, e.Message);
                }
            }

            return Task.CompletedTask;
        }

        private async Task<Task> OnComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreateEventArgs e)
        {
            if (e.Id == "verify_button" || e.Id == "accept_button" || e.Id == "deny_button" || e.Id == "why_button")
            {
                await HandleMiscInteractivity.HandleVerificationRequests(sender, e);
            }

            else if (e.Id == "roles_button" || e.Id.Contains("roles_dropdown"))
            {
                await HandleMiscInteractivity.HandleRolesInteraction(sender, e);
            }

            Client.Logger.LogInformation(EventIds.EventHandler, $"User {e.User.Username}#{e.User.Discriminator} ({e.User.Id}) interacted with '{e.Id}' in #{e.Channel.Name} ({e.Channel.Id}).", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task OnMessageReactionAdded(object sender, MessageReactionAddEventArgs e)
        {
            Client.Logger.LogInformation(EventIds.EventHandler,
                $"User '{e.User.Username}#{e.User.Discriminator}' ({e.User.Id}) " +
                $"added '{e.Emoji}' in #{e.Channel.Name} ({e.Channel.Id}).",
                DateTime.Now);

            return Task.CompletedTask;
        }

        private Task OnMessageReactionRemoved(DiscordClient sender, MessageReactionRemoveEventArgs e)
        {
            Client.Logger.LogInformation(EventIds.EventHandler,
                $"User '{e.User.Username}#{e.User.Discriminator}' ({e.User.Id}) " +
                $"removed '{e.Emoji}' in #{e.Channel.Name} ({e.Channel.Id}).",
                DateTime.Now);

            return Task.CompletedTask;
        }

        private Task OnGuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs e)
        {
            sender.Logger.LogInformation(EventIds.EventHandler, $"User added: {e.Member.Username}#{e.Member.Discriminator} ({e.Member.Id}) in {e.Guild.Name} ({e.Guild.Id}).", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task OnGuildMemberRemoved(DiscordClient sender, GuildMemberRemoveEventArgs e)
        {
            sender.Logger.LogInformation(EventIds.EventHandler, $"User removed: {e.Member.Username}#{e.Member.Discriminator} ({e.Member.Id}) in {e.Guild.Name} ({e.Guild.Id}).", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task OnGuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
        {
            sender.Logger.LogInformation(EventIds.EventHandler, $"Guild available: {e.Guild.Name} ({e.Guild.Id}).", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task OnGuildCreated(DiscordClient sender, GuildCreateEventArgs e)
        {
            sender.Logger.LogInformation(EventIds.EventHandler, $"Guild added: {e.Guild.Name} ({e.Guild.Id}).", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task OnGuildDeleted(DiscordClient sender, GuildDeleteEventArgs e)
        {
            sender.Logger.LogInformation(EventIds.EventHandler, $"Guild removed: {e.Guild.Name} ({e.Guild.Id}).", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task OnSocketErrored(DiscordClient sender, SocketErrorEventArgs e)
        {
            var ex = e.Exception;
            while (ex is AggregateException)
                ex = ex.InnerException;

            sender.Logger.LogCritical(EventIds.Core, e.Exception, $"Socket threw an exception.", DateTime.Now);

            if (ex.Message is "Could not connect to Discord.")
            {
                sender.Logger.LogInformation(EventIds.Core, "Terminating...", DateTime.Now);

                Environment.Exit(0);
            }

            return Task.CompletedTask;
        }

        private Task OnClientErrored(DiscordClient sender, ClientErrorEventArgs e)
        {
            sender.Logger.LogWarning(EventIds.Core, e.Exception, $"Client threw an exception.", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task OnHeartbeated(DiscordClient sender, HeartbeatEventArgs e)
        {
            SharedData.ReceivedHeartbeats++;

            sender.Logger.LogInformation(EventIds.Core, $"Received heartbeat ACK: {e.Ping} ms.", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task OnUnknownEvent(DiscordClient sender, UnknownEventArgs e)
        {
            e.Handled = true;

            sender.Logger.LogWarning(EventIds.Core, $"Received unknown event {e.EventName}, payload:\n{e.Json}", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task CommandsNext_CommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e)
        {
            e.Context.Client.Logger.LogInformation(EventIds.CommandHandler,
                $"User '{e.Context.User.Username}#{e.Context.User.Discriminator}' ({e.Context.User.Id}) " +
                $"executed '{e.Command.QualifiedName}' in #{e.Context.Channel.Name} ({e.Context.Channel.Id}) guild '{e.Context.Guild.Name}' ({e.Context.Guild.Id}).",
                DateTime.Now);

            return Task.CompletedTask;
        }

        private async Task<Task> CommandsNext_CommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            try
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
                        await e.Context.RespondAsync($"{Formatter.Bold("[ERROR]")} This command is restricted to members with administrator permissions or server owner only.");
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
            }

            catch
            {
                if (e.Exception is not CommandNotFoundException)
                {
                    DiscordEmbedBuilder errorEmbed = new DiscordEmbedBuilder
                    {
                        Title = "An error occurred!",
                        Description = $"Details: {Formatter.InlineCode($"{e.Exception.Message}")}",
                        Timestamp = DateTime.Now,
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = "OSIS Discord Assistant"
                        },
                        Color = DiscordColor.MidnightBlue
                    };

                    await e.Context.RespondAsync(embed: errorEmbed.Build());
                }               
            }

            e.Context.Client.Logger.LogError(EventIds.CommandHandler, e.Exception,
                $"User '{e.Context.User.Username}#{e.Context.User.Discriminator}' ({e.Context.User.Id}) tried to execute '{e.Command?.QualifiedName ?? "<unknown command>"}' "
                + $"in #{e.Context.Channel.Name} ({e.Context.Channel.Id}) guild '{e.Context.Guild.Name}' ({e.Context.Guild.Id}) and failed.", DateTime.Now);

            return Task.CompletedTask;
        }
    }
}
