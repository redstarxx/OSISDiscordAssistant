using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OSISDiscordAssistant.Attributes;
using OSISDiscordAssistant.Constants;
using OSISDiscordAssistant.Services;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;

namespace OSISDiscordAssistant
{
    public class EventHandlers
    {
        private readonly ILogger<EventHandlers> _logger;
        private readonly IMainGuildStaticInteractionHandler _mainGuildStaticInteractionHandler;
        private readonly IEventReminderService _eventReminderService;
        private readonly IProposalReminderService _proposalReminderService;
        private readonly IVerificationCleanupService _verificationCleanupService;
        private readonly IStatusUpdaterService _statusUpdaterService;
        private readonly IHeartbeatMonitoringService _heartbeatMonitoringService;
        private readonly IReminderService _reminderService;

        public EventHandlers(ILogger<EventHandlers> logger, 
            IMainGuildStaticInteractionHandler mainGuildStaticInteractionHandler, 
            IEventReminderService eventReminderService, 
            IProposalReminderService proposalReminderService, 
            IVerificationCleanupService verificationCleanupService, 
            IStatusUpdaterService statusUpdaterService, 
            IHeartbeatMonitoringService heartbeatMonitoringService,
            IReminderService reminderService)
        {
            _logger = logger;
            _mainGuildStaticInteractionHandler = mainGuildStaticInteractionHandler;
            _eventReminderService = eventReminderService;
            _proposalReminderService = proposalReminderService;
            _verificationCleanupService = verificationCleanupService;
            _statusUpdaterService = statusUpdaterService;
            _heartbeatMonitoringService = heartbeatMonitoringService;
            _reminderService = reminderService;
        }

        public Task OnClientReady(object sender, ReadyEventArgs e)
        {
            return Task.CompletedTask;
        }

        public Task OnGuildDownloadCompleted(object sender, GuildDownloadCompletedEventArgs e)
        {
            _eventReminderService.Start();

            _proposalReminderService.Start();

            _verificationCleanupService.Start();

            _statusUpdaterService.Start();

            _heartbeatMonitoringService.Start();

            _reminderService.Start();

            _logger.LogInformation(EventIds.Core, "Client is ready for tasking.");

            return Task.CompletedTask;
        }

        public Task OnMessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {
            if (e.Channel.IsPrivate && !e.Author.IsCurrent)
            {
                _logger.LogInformation("User '{Username}#{Discriminator}' ({UserId}) sent \"{MessageContent}\" through Direct Messages ({ChannelId})", e.Author.Username, e.Author.Discriminator, e.Author.Id, e.Message.Content, e.Channel.Id);
            }

            return Task.CompletedTask;
        }

        public Task OnMessageUpdated(DiscordClient sender, MessageUpdateEventArgs e)
        {
            if (e.Author == sender.CurrentUser)
            {
                return Task.CompletedTask;
            }

            _logger.LogInformation(EventIds.EventHandler,
                "User '{Username}#{Discriminator}' ({UserId}) updated message ({MessageId}) in #{ChannelName} ({ChannelId}) guild '{GuildName}' ({GuildId}).", e.Message.Author.Username, e.Message.Author.Discriminator, e.Message.Author.Id, e.Message.Id, e.Channel.Name, e.Channel.Id, e.Guild.Name, e.Guild.Id);

            if (e.MessageBefore is null)
            {
                _logger.LogInformation(EventIds.Core, "Message ({MessageId}) is not cached. Aborted storing previous message content.", e.Message.Id);

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

        public Task OnMessageDeleted(DiscordClient sender, MessageDeleteEventArgs e)
        {
            if (e.Message.Author == sender.CurrentUser)
            {
                return Task.CompletedTask;
            }

            try
            {
                _logger.LogInformation(EventIds.EventHandler,
                    "User '{Username}#{Discriminator}' ({UserId}) deleted message ({MessageId}) in #{ChannelName} ({ChannelId}) guild '{GuildName}' ({GuildId}).", e.Message.Author.Username, e.Message.Author.Discriminator, e.Message.Author.Id, e.Message.Id, e.Channel.Name, e.Channel.Id, e.Guild.Name, e.Guild.Id);
            }

            catch
            {
                _logger.LogInformation(EventIds.Core, "Message ({MessageId}) was not cached. Skipped logging message deleted event.", e.Message.Id);

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

        public Task OnComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreateEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                _logger.LogInformation("User {Username}#{Discriminator} ({UserId}) interacted with '{ComponentId}' in #{ChannelName} ({ChannelId}).", e.User.Username, e.User.Discriminator, e.User.Id, e.Id, e.Channel.Name, e.Channel.Id);

                if (e.Id == "verify_button" || e.Id == "accept_button" || e.Id == "deny_button" || e.Id == "why_button")
                {
                    await _mainGuildStaticInteractionHandler.HandleVerificationRequests(sender, e);
                }

                else if (e.Id == "roles_button" || e.Id.Contains("roles_dropdown"))
                {
                    await _mainGuildStaticInteractionHandler.HandleRolesInteraction(sender, e);
                }
            });

            return Task.CompletedTask;
        }

        public Task OnMessageReactionAdded(object sender, MessageReactionAddEventArgs e)
        {
            _logger.LogInformation(EventIds.EventHandler,
                "User '{Username}#{Discriminator}' ({UserId}) added '{Emoji}' in #{ChannelName} ({ChannelId}).", e.User.Username, e.User.Discriminator, e.User.Id, e.Emoji, e.Channel.Name, e.Channel.Id);

            return Task.CompletedTask;
        }

        public Task OnMessageReactionRemoved(DiscordClient sender, MessageReactionRemoveEventArgs e)
        {
            _logger.LogInformation(EventIds.EventHandler,
                "User '{Username}#{Discriminator}' ({UserId}) removed '{Emoji}' in #{ChannelName} ({ChannelId}).", e.User.Username, e.User.Discriminator, e.User.Id, e.Emoji, e.Channel.Name, e.Channel.Id);

            return Task.CompletedTask;
        }

        public Task OnGuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs e)
        {
            _logger.LogInformation("User added: {Username}#{Discriminator} ({UserId}) in {GuildName} ({GuildId}).", e.Member.Username, e.Member.Discriminator, e.Member.Id, e.Guild.Name, e.Guild.Id);

            return Task.CompletedTask;
        }

        public Task OnGuildMemberRemoved(DiscordClient sender, GuildMemberRemoveEventArgs e)
        {
            _logger.LogInformation("User removed: {Username}#{Discriminator} ({UserId}) in {GuildName} ({GuildId}).", e.Member.Username, e.Member.Discriminator, e.Member.Id, e.Guild.Name, e.Guild.Id);

            return Task.CompletedTask;
        }

        public Task OnGuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
        {
            _logger.LogInformation("Guild available: {GuildName} ({GuildId}).", e.Guild.Name, e.Guild.Id);

            return Task.CompletedTask;
        }

        public Task OnGuildCreated(DiscordClient sender, GuildCreateEventArgs e)
        {
            _logger.LogInformation("Guild added: {GuildName} ({GuildId}).", e.Guild.Name, e.Guild.Id);

            return Task.CompletedTask;
        }

        public Task OnGuildDeleted(DiscordClient sender, GuildDeleteEventArgs e)
        {
            _logger.LogInformation("Guild removed: {GuildName} ({GuildId}).", e.Guild.Name, e.Guild.Id);

            return Task.CompletedTask;
        }

        public Task OnSocketErrored(DiscordClient sender, SocketErrorEventArgs e)
        {
            var ex = e.Exception;
            while (ex is AggregateException)
                ex = ex.InnerException;

            _logger.LogCritical(EventIds.Core, e.Exception, "Socket threw an exception.");

            if (ex.Message is "Could not connect to Discord.")
            {
                _logger.LogInformation(EventIds.Core, "Terminating...");

                Environment.Exit(0);
            }

            return Task.CompletedTask;
        }

        public Task OnClientErrored(DiscordClient sender, ClientErrorEventArgs e)
        {
            _logger.LogWarning(EventIds.Core, e.Exception, "Client threw an exception.");

            return Task.CompletedTask;
        }

        public Task OnHeartbeated(DiscordClient sender, HeartbeatEventArgs e)
        {
            SharedData.ReceivedHeartbeats++;

            _logger.LogInformation(EventIds.Core, "Received heartbeat ACK: {RoundTripTime} ms.", e.Ping);

            return Task.CompletedTask;
        }

        public Task OnUnknownEvent(DiscordClient sender, UnknownEventArgs e)
        {
            e.Handled = true;

            _logger.LogWarning(EventIds.Core, "Received unknown event {EventName}, payload:\n{JsonPayload}", e.EventName, e.Json);

            return Task.CompletedTask;
        }

        public Task CommandsNext_CommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e)
        {
            _logger.LogInformation(EventIds.CommandHandler,
                "User '{Username}#{Discriminator}' ({UserId}) executed '{CommandQualifiedName}' in #{ChannelName} ({ChannelId}) guild '{GuildName}' ({GuildId}).", e.Context.User.Username, e.Context.User.Discriminator, e.Context.User.Id, e.Command.QualifiedName, e.Context.Channel.Name, e.Context.Channel.Id, e.Context.Guild.Name, e.Context.Guild.Id);

            return Task.CompletedTask;
        }

        public async Task<Task> CommandsNext_CommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            try
            {
                var failedChecks = ((ChecksFailedException)e.Exception).FailedChecks;
                foreach (var failedCheck in failedChecks)
                {
                    if (failedCheck is RequireMainGuild)
                    {
                        await e.Context.RespondAsync($"{Formatter.Bold("[ERROR]")} This command is only usable in the OSIS Sekolah Djuwita Batam Discord server!\nInvite Link: {SharedData.MainGuildInviteLink}");
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

            _logger.LogError(EventIds.CommandHandler, e.Exception,
                "User '{Username}#{Discriminator}' ({UserId}) tried to execute '{CommandQualifiedName}' in #{ChannelName} ({ChannelId}) guild '{GuildName}' ({GuildId}) and failed.", e.Context.User.Username, e.Context.User.Discriminator, e.Context.User.Id, e.Command?.QualifiedName ?? "<unknown command>", e.Context.Channel.Name, e.Context.Channel.Id, e.Context.Guild.Name, e.Context.Guild.Id);

            return Task.CompletedTask;
        }
    }
}
