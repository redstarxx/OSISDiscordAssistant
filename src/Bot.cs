using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using System;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using discordbot.Commands;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Entities;
using System.Linq;
using System.Globalization;
using System.Diagnostics;
using Humanizer;

namespace discordbot
{
    public class Bot
    {
        public static DiscordClient Client { get; private set; }

        public InteractivityExtension Interactivity { get; private set; }

        public CommandsNextExtension Commands { get; private set; }

        public static EventId LogEvent { get; } = new EventId(1000, "BotClient");

        public static EventId ERTask { get; } = new EventId(2000, "ERTask");

        public static EventId PRTask { get; } = new EventId(3000, "PRTask");

        public static EventId StatusUpdater { get; } = new EventId(4000, "StatusUpdater");

        public async Task RunAsync()
        {
            var json = string.Empty;
            using (var fileString = File.OpenRead("config.json"))
            using (var stringReader = new StreamReader(fileString, new UTF8Encoding(false)))
                json = await stringReader.ReadToEndAsync().ConfigureAwait(false);

            var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);

            var config = new DiscordConfiguration
            {
                Token = configJson.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                MinimumLogLevel = LogLevel.Information,
                Intents = DiscordIntents.All
            };

            Client = new DiscordClient(config);
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

            Client.UseInteractivity(new InteractivityConfiguration
            {
                Timeout = TimeSpan.FromDays(7)
            });

            var commandsConfig = new CommandsNextConfiguration
            {
                StringPrefixes = new String[] { configJson.Prefix },
                EnableMentionPrefix = true,
                EnableDms = false,
                EnableDefaultHelp = false
            };

            Commands = Client.UseCommandsNext(commandsConfig);

            // Registers commands.
            Commands.RegisterCommands<MiscCommandsModule>();
            Commands.RegisterCommands<VerificationCommandsModule>();
            Commands.RegisterCommands<ServerAdministrationCommandsModule>();
            Commands.RegisterCommands<ReminderCommandsModule>();
            Commands.RegisterCommands<EventCommandsModule>();
            Commands.RegisterCommands<HelpCommandsModule>();
            Commands.RegisterCommands<BotAdministrationCommands>();
            Commands.RegisterCommands<PollCommandsModule>();
            Commands.RegisterCommands<TagsCommandsModule>();

            // Registers event handlers.
            Commands.CommandExecuted += CommandsNext_CommandExecuted;
            Commands.CommandErrored += CommandsNext_CommandErrored;

            // Displays the current version of the bot.
            Client.Logger.LogInformation(LogEvent, $"DiscordBotOSIS v{ClientUtilities.GetBuildVersion()}", ClientUtilities.GetWesternIndonesianDateTime());

            // Tell that whoever is seeing this that the client is connecting to Discord's gateway.
            Client.Logger.LogInformation(LogEvent, "Connecting to Discord's gateway...", ClientUtilities.GetWesternIndonesianDateTime());
            await Client.ConnectAsync();
            await Task.Delay(-1);
        }

        private Task OnClientReady(object sender, ReadyEventArgs e)
        {
            return Task.CompletedTask;
        }

        private Task OnGuildDownloadCompleted(object sender, GuildDownloadCompletedEventArgs e)
        {
            // Starts a new Discord status updater task to change the bot's display status every two minutes.
            StartStatusUpdater();

            // Starts the events reminder task which queries the events table on a minute-by-minute basis.
            EventReminders();

            // Starts the proposals reminder task which queries the events table on a daily basis.
            // Reminders are sent 30 days before or a week before the day of the event.
            ProposalReminders();

            Client.Logger.LogInformation(LogEvent, "Client is ready for tasking.", ClientUtilities.GetWesternIndonesianDateTime());

            return Task.CompletedTask;
        }

        private Task EventReminders()
        {
            Task eventReminder = Task.Run(async () =>
            {
                DiscordChannel eventsChannel = await Client.GetChannelAsync(857589614558314575);

                DiscordChannel errorLogsChannel = await Client.GetChannelAsync(832172186126123029);

                var reminderEmbed = new DiscordEmbedBuilder
                {
                    Timestamp = ClientUtilities.GetWesternIndonesianDateTime(),
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = "OSIS Discord Assistant"
                    },
                    Color = DiscordColor.MidnightBlue
                };

                DiscordMessageBuilder reminderMessageBuilder = new DiscordMessageBuilder
                {
                };

                Stopwatch stopwatch = new Stopwatch();

                try
                {
                    while (true)
                    {
                        int counter = 0;

                        stopwatch.Start();
                        using (var db = new EventContext())
                        {
                            foreach (var row in db.Events)
                            {
                                var cultureInfo = new CultureInfo(row.EventDateCultureInfo);

                                // Add 7 hours ahead because for some reason Linux doesn't pick the user preferred timezone.
                                DateTime currentDateTime = ClientUtilities.GetWesternIndonesianDateTime();

                                DateTime parseEventDateTime = DateTime.Parse(row.EventDate, cultureInfo);

                                TimeSpan timeSpan = parseEventDateTime - currentDateTime;

                                if (timeSpan.Days == 7)
                                {
                                    if (row.PreviouslyReminded == false)
                                    {
                                        reminderEmbed.Title = $"Events Manager - Reminding {row.EventName}... (Event ID: {row.Id})";
                                        reminderEmbed.Description = $"Attention council members! Next week will be the day for {Formatter.Bold(row.EventName)}! Read below to find out more about this event.";

                                        reminderEmbed.AddField("Ketua / Wakil Ketua Event", row.PersonInCharge, true);
                                        reminderEmbed.AddField("Tanggal / Waktu Pelaksanaan", row.EventDate, true);
                                        reminderEmbed.AddField("Informasi Tambahan", row.EventDescription, true);

                                        reminderMessageBuilder.WithContent("@everyone")
                                                              .WithEmbed(embed: reminderEmbed);

                                        await eventsChannel.SendMessageAsync(builder: reminderMessageBuilder);
                                        counter++;

                                        using (var dbUpdate = new EventContext())
                                        {
                                            Events _d = null;
                                            _d = dbUpdate.Events.SingleOrDefault(x => x.Id == row.Id);

                                            if (_d != null)
                                            {
                                                _d.PreviouslyReminded = true;
                                            }

                                            dbUpdate.SaveChanges();
                                        }
                                    }
                                }

                                else if (timeSpan.Days < 7 && timeSpan.Days > 1)
                                {
                                    if (row.PreviouslyReminded == false)
                                    {
                                        reminderEmbed.Title = $"Events Manager - Reminding {row.EventName}... (Event ID: {row.Id})";
                                        reminderEmbed.Description = $"Attention council members! In {timeSpan.Days} day(s), it will be the day for {Formatter.Bold(row.EventName)}! Read below to find out more about this event.";

                                        reminderEmbed.AddField("Ketua / Wakil Ketua Event", row.PersonInCharge, true);
                                        reminderEmbed.AddField("Tanggal / Waktu Pelaksanaan", row.EventDate, true);
                                        reminderEmbed.AddField("Informasi Tambahan", row.EventDescription, true);

                                        reminderMessageBuilder.WithContent("@everyone")
                                                              .WithEmbed(embed: reminderEmbed);

                                        await eventsChannel.SendMessageAsync(builder: reminderMessageBuilder);
                                        counter++;

                                        using (var dbUpdate = new EventContext())
                                        {
                                            Events _d = null;
                                            _d = dbUpdate.Events.SingleOrDefault(x => x.Id == row.Id);

                                            if (_d != null)
                                            {
                                                _d.PreviouslyReminded = true;
                                            }

                                            dbUpdate.SaveChanges();
                                        }
                                    }
                                }

                                else if (timeSpan.Days == 1)
                                {
                                    if (row.PreviouslyReminded == false)
                                    {
                                        reminderEmbed.Title = $"Events Manager - Reminding {row.EventName}... (Event ID: {row.Id})";
                                        reminderEmbed.Description = $"Attention council members! Tomorrow will be the day for {Formatter.Bold(row.EventName)}! Read below to find out more about this event.";

                                        reminderEmbed.AddField("Ketua / Wakil Ketua Event", row.PersonInCharge, true);
                                        reminderEmbed.AddField("Tanggal / Waktu Pelaksanaan", row.EventDate, true);
                                        reminderEmbed.AddField("Informasi Tambahan", row.EventDescription, true);

                                        reminderMessageBuilder.WithContent("@everyone")
                                                              .WithEmbed(embed: reminderEmbed);

                                        await eventsChannel.SendMessageAsync(builder: reminderMessageBuilder);
                                        counter++;

                                        using (var dbUpdate = new EventContext())
                                        {
                                            Events _d = null;
                                            _d = dbUpdate.Events.SingleOrDefault(x => x.Id == row.Id);

                                            if (_d != null)
                                            {
                                                _d.PreviouslyReminded = true;
                                            }

                                            dbUpdate.SaveChanges();
                                        }
                                    }
                                }

                                else if (timeSpan.Days < 1)
                                {
                                    if (row.PreviouslyReminded == false)
                                    {
                                        reminderEmbed.Title = $"Events Manager - Reminding {row.EventName}... (Event ID: {row.Id})";
                                        reminderEmbed.Description = $"Attention council members! {Formatter.Bold(row.EventName)} will be in effect in {timeSpan.Hours} hours! Read below to find out more about this event.";

                                        reminderEmbed.AddField("Ketua / Wakil Ketua Event", row.PersonInCharge, true);
                                        reminderEmbed.AddField("Tanggal / Waktu Pelaksanaan", row.EventDate, true);
                                        reminderEmbed.AddField("Informasi Tambahan", row.EventDescription, true);

                                        reminderMessageBuilder.WithContent("@everyone")
                                                              .WithEmbed(embed: reminderEmbed);

                                        await eventsChannel.SendMessageAsync(builder: reminderMessageBuilder);
                                        counter++;

                                        using (var dbUpdate = new EventContext())
                                        {
                                            Events _d = null;
                                            _d = dbUpdate.Events.SingleOrDefault(x => x.Id == row.Id);

                                            if (_d != null)
                                            {
                                                _d.PreviouslyReminded = true;
                                            }

                                            dbUpdate.SaveChanges();
                                        }
                                    }
                                }

                                if (parseEventDateTime.ToShortDateString() == currentDateTime.ToShortDateString())
                                {
                                    if (row.Expired == false)
                                    {
                                        reminderEmbed.Title = $"Events Manager - Reminding {row.EventName}... (Event ID: {row.Id})";
                                        reminderEmbed.Description = $"Attention council members! Today is the day for {Formatter.Bold(row.EventName)}! Read the description below to know more.";

                                        reminderEmbed.AddField("Ketua / Wakil Ketua Event", row.PersonInCharge, true);
                                        reminderEmbed.AddField("Tanggal / Waktu Pelaksanaan", row.EventDate, true);
                                        reminderEmbed.AddField("Informasi Tambahan", row.EventDescription, true);

                                        reminderMessageBuilder.WithContent("@everyone")
                                                              .WithEmbed(embed: reminderEmbed);

                                        await eventsChannel.SendMessageAsync(builder: reminderMessageBuilder);
                                        counter++;

                                        using (var dbUpdate = new EventContext())
                                        {
                                            Events _d = null;
                                            _d = dbUpdate.Events.SingleOrDefault(x => x.Id == row.Id);

                                            if (_d != null)
                                            {
                                                _d.PreviouslyReminded = true;
                                                _d.Expired = true;
                                            }

                                            dbUpdate.SaveChanges();
                                        }
                                    }                                    
                                }
                            }

                            await db.DisposeAsync();
                        }

                        stopwatch.Stop();
                        long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                        if (counter != 0)
                        {
                            Client.Logger.LogInformation(ERTask, $"It took {elapsedMilliseconds} milliseconds to complete the minute-by-minute basis events reminder task. Reminded {counter.ToString()} ({counter.ToWords()}) events.", ClientUtilities.GetWesternIndonesianDateTime());
                        }

                        else
                        {
                            Client.Logger.LogInformation(ERTask, $"It took {elapsedMilliseconds} milliseconds to complete the minute-by-minute basis events reminder task. No events to remind.", ClientUtilities.GetWesternIndonesianDateTime());
                        }

                        stopwatch.Reset();
                        await Task.Delay(TimeSpan.FromMinutes(1));
                    }
                }

                catch (Exception ex)
                {
                    await errorLogsChannel.SendMessageAsync($"{ex.Message}").ConfigureAwait(false);
                }
            });

            Client.Logger.LogInformation(ERTask, "Initialized event reminders task.", ClientUtilities.GetWesternIndonesianDateTime());

            return Task.CompletedTask;
        }

        private Task ProposalReminders()
        {
            Task eventReminder = Task.Run(async () =>
            {
                DiscordChannel eventsChannel = await Client.GetChannelAsync(857589664269729802);

                DiscordChannel errorLogsChannel = await Client.GetChannelAsync(832172186126123029);

                var reminderEmbed = new DiscordEmbedBuilder
                {
                    Timestamp = ClientUtilities.GetWesternIndonesianDateTime(),
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = "OSIS Discord Assistant"
                    },
                    Color = DiscordColor.MidnightBlue
                };

                DiscordMessageBuilder reminderMessageBuilder = new DiscordMessageBuilder
                {
                };

                Stopwatch stopwatch = new Stopwatch();

                try
                {
                    while (true)
                    {
                        int counter = 0;

                        stopwatch.Start();
                        using (var db = new EventContext())
                        {
                            foreach (var row in db.Events)
                            {
                                var cultureInfo = new CultureInfo(row.EventDateCultureInfo);

                                // Add 7 hours ahead because for some reason Linux doesn't pick the user preferred timezone.
                                DateTime currentDateTime = ClientUtilities.GetWesternIndonesianDateTime();

                                DateTime parseEventDateTime = DateTime.Parse(row.EventDate, cultureInfo);

                                TimeSpan timeSpan = parseEventDateTime - currentDateTime;

                                if (timeSpan.Days == 30 || timeSpan.Days > 6)
                                {
                                    if (row.ProposalReminded == false)
                                    {
                                        reminderEmbed.Title = $"Events Manager - Proposal Submission for {row.EventName}... (Event ID: {row.Id})";
                                        reminderEmbed.Description = $"Make sure you have submitted your respective proposals in preparation for {Formatter.Bold(row.EventName)}!";

                                        reminderEmbed.AddField("Tanggal / Waktu Pelaksanaan", row.EventDate, true);
                                        reminderEmbed.AddField("Informasi Tambahan", row.EventDescription, true);

                                        reminderMessageBuilder.WithContent(row.PersonInCharge)
                                                              .WithEmbed(embed: reminderEmbed);

                                        await eventsChannel.SendMessageAsync(builder: reminderMessageBuilder);
                                        counter++;

                                        using (var dbUpdate = new EventContext())
                                        {
                                            Events _d = null;
                                            _d = dbUpdate.Events.SingleOrDefault(x => x.Id == row.Id);

                                            if (_d != null)
                                            {
                                                _d.ProposalReminded = true;
                                            }

                                            dbUpdate.SaveChanges();
                                        }
                                    }
                                }

                                if (parseEventDateTime.ToShortDateString() == currentDateTime.ToShortDateString())
                                {
                                    if (row.ProposalReminded == false)
                                    {
                                        using (var dbUpdate = new EventContext())
                                        {
                                            Events _d = null;
                                            _d = dbUpdate.Events.SingleOrDefault(x => x.Id == row.Id);

                                            if (_d != null)
                                            {
                                                _d.ProposalReminded = true;
                                            }

                                            dbUpdate.SaveChanges();
                                        }
                                    }
                                }
                            }

                            await db.DisposeAsync();
                        }

                        stopwatch.Stop();
                        long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                        if (counter != 0)
                        {
                            Client.Logger.LogInformation(PRTask, $"It took {elapsedMilliseconds} milliseconds to complete the proposal submission reminder task. Reminded {counter.ToString()} ({counter.ToWords()}) proposal submissions.", ClientUtilities.GetWesternIndonesianDateTime());
                        }

                        else
                        {
                            Client.Logger.LogInformation(PRTask, $"It took {elapsedMilliseconds} milliseconds to complete the proposal submission reminder task. No proposal submissions to remind.", ClientUtilities.GetWesternIndonesianDateTime());
                        }

                        stopwatch.Reset();
                        await Task.Delay(TimeSpan.FromMinutes(1));
                    }
                }

                catch (Exception ex)
                {
                    await errorLogsChannel.SendMessageAsync($"{ex.Message}").ConfigureAwait(false);
                }
            });

            Client.Logger.LogInformation(PRTask, "Initialized proposal reminders task.", ClientUtilities.GetWesternIndonesianDateTime());

            return Task.CompletedTask;
        }

        public static void StartStatusUpdater()
        {
            Task statusUpdater = Task.Run(async () =>
            {
                string gradeNumber = "VII";

                while (true)
                {
                    var activity = new DiscordActivity("Grade " + gradeNumber, ActivityType.Watching);
                    await Client.UpdateStatusAsync(activity);

                    switch (gradeNumber)
                    {
                        case "VII":
                            gradeNumber = "VIII";
                            break;
                        case "VIII":
                            gradeNumber = "IX";
                            break;
                        case "IX":
                            gradeNumber = "X SCIENCE";
                            break;
                        case "X SCIENCE":
                            gradeNumber = "X SOCIAL";
                            break;
                        case "X SOCIAL":
                            gradeNumber = "XI SCIENCE";
                            break;
                        case "XI SCIENCE":
                            gradeNumber = "XI SOCIAL";
                            break;
                        case "XI SOCIAL":
                            gradeNumber = "XII SCIENCE";
                            break;
                        case "XII SCIENCE":
                            gradeNumber = "XII SOCIAL";
                            break;
                        case "XII SOCIAL":
                            gradeNumber = "VII";
                            break;
                        default:
                            break;
                    }
                    
                    await Task.Delay(TimeSpan.FromMinutes(2));
                }
            });

            Client.Logger.LogInformation(StatusUpdater, "Initialized status updater task.", ClientUtilities.GetWesternIndonesianDateTime());
        }

        private Task OnMessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {
            if (e.Channel.IsPrivate && !e.Author.IsCurrent)
            {
                Client.Logger.LogInformation(LogEvent, $"User '{e.Author.Username}#{e.Author.Discriminator}' ({e.Author.Id}) sent \"{e.Message.Content}\" through Direct Messages ({e.Channel.Id})", ClientUtilities.GetWesternIndonesianDateTime());

                if (e.Message.Content.StartsWith("!"))
                {
                    string toSend = $"{Formatter.Bold("[ERROR]")} Sorry, you can only execute commands in the OSIS Discord server!";

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

            DiscordChannel welcomeChannel = e.Guild.GetChannel(814450803464732722);

            string toSend = $"selamat datang {e.Member.Mention}! {DiscordEmoji.FromName(Client, ":omculikaku:")}";

            welcomeChannel.SendMessageAsync(toSend).ConfigureAwait(false);

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

        private Task CommandsNext_CommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e)
        {
            e.Context.Client.Logger.LogInformation(LogEvent,
                $"User '{e.Context.User.Username}#{e.Context.User.Discriminator}' ({e.Context.User.Id}) " +
                $"executed '{e.Command.QualifiedName}' in #{e.Context.Channel.Name} ({e.Context.Channel.Id})",
                ClientUtilities.GetWesternIndonesianDateTime());

            return Task.CompletedTask;
        }

        private Task CommandsNext_CommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            e.Context.Client.Logger.LogError(LogEvent,
                $"User '{e.Context.User.Username}#{e.Context.User.Discriminator}' ({e.Context.User.Id}) tried to execute '{e.Command?.QualifiedName ?? "<unknown command>"}' "
                + $"in #{e.Context.Channel.Name} ({e.Context.Channel.Id}) and failed with {e.Exception.GetType()}: {e.Exception.Message}", ClientUtilities.GetWesternIndonesianDateTime());

            return Task.CompletedTask;
        }
    }
}
