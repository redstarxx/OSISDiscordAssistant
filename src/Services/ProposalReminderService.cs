using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Humanizer;
using Microsoft.Extensions.Logging;
using OSISDiscordAssistant.Constants;
using OSISDiscordAssistant.Models;

namespace OSISDiscordAssistant.Services
{
    public class ProposalReminderService : IProposalReminderService
    {
        private readonly ILogger<ProposalReminderService> _logger;
        private readonly EventContext _eventContext;
        private readonly DiscordShardedClient _shardedClient;

        public ProposalReminderService(ILogger<ProposalReminderService> logger, EventContext eventContext, DiscordShardedClient shardedClient)
        {
            _logger = logger;
            _eventContext = eventContext;
            _shardedClient = shardedClient;
        }

        public void Start()
        {
            if (SharedData.IsProposalReminderInitialized)
            {
                _logger.LogInformation("Proposal reminder task is already fired. Skipping initialization.", DateTime.Now);

                return;
            }

            Task proposalReminder = Task.Run(async () =>
            {
                DiscordChannel proposalChannel = await _shardedClient.GetShard(SharedData.MainGuildId).GetChannelAsync(SharedData.ProposalChannelId);

                DiscordChannel errorLogsChannel = await _shardedClient.GetShard(SharedData.MainGuildId).GetChannelAsync(SharedData.ErrorChannelId);

                DiscordMessageBuilder reminderMessageBuilder = new DiscordMessageBuilder
                {
                };

                Stopwatch stopwatch = new Stopwatch();

                try
                {
                    while (true)
                    {

                        var reminderEmbed = new DiscordEmbedBuilder
                        {
                            Timestamp = DateTime.Now,
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = "OSIS Discord Assistant"
                            },
                            Color = DiscordColor.MidnightBlue
                        };

                        int counter = 0;

                        stopwatch.Start();

                        Stopwatch processingStopWatch = new Stopwatch();

                        bool sentReminder = false;

                        foreach (var row in _eventContext.Events)
                        {
                            processingStopWatch.Start();

                            var cultureInfo = new CultureInfo(row.EventDateCultureInfo);

                            DateTime currentDateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

                            DateTime parsedEventDateTime = DateTime.Parse(row.EventDate, cultureInfo);

                            TimeSpan timeSpan = parsedEventDateTime - currentDateTime;

                            double remainingDays = Math.Round(timeSpan.TotalDays);

                            if (remainingDays == 30 || remainingDays > 6 && remainingDays < 30)
                            {
                                if (row.ProposalReminded == false)
                                {
                                    reminderEmbed.Title = $"Events Manager - Proposal Submission for {row.EventName}... (Event ID: {row.Id})";
                                    reminderEmbed.Description = $"Make sure you have submitted your respective proposals in preparation for {Formatter.Bold(row.EventName)}!";

                                    reminderEmbed.AddField("Tanggal / Waktu Pelaksanaan", row.EventDate, true);
                                    reminderEmbed.AddField("Informasi Tambahan", row.EventDescription, true);

                                    reminderMessageBuilder.WithContent(row.PersonInCharge)
                                                          .WithEmbed(embed: reminderEmbed);

                                    await proposalChannel.SendMessageAsync(builder: reminderMessageBuilder);
                                    counter++;

                                    Events _d = null;
                                    _d = _eventContext.Events.SingleOrDefault(x => x.Id == row.Id);

                                    if (_d != null)
                                    {
                                        _d.ProposalReminded = true;
                                    }

                                    _eventContext.SaveChanges();

                                    sentReminder = true;
                                }
                            }

                            if (parsedEventDateTime.ToShortDateString() == currentDateTime.ToShortDateString())
                            {
                                if (row.ProposalReminded == false)
                                {
                                    Events _d = null;
                                    _d = _eventContext.Events.SingleOrDefault(x => x.Id == row.Id);

                                    if (_d != null)
                                    {
                                        _d.ProposalReminded = true;
                                    }

                                    _eventContext.SaveChanges();

                                    _logger.LogInformation($"Marked '{row.EventName}' (ID: {row.Id}) proposal reminded column as {row.ProposalReminded}.", DateTime.Now);
                                }
                            }

                            processingStopWatch.Stop();

                            if (sentReminder)
                            {
                                sentReminder = false;

                                _logger.LogInformation($"Sent proposal reminder for '{row.EventName}' (ID: {row.Id}) in {processingStopWatch.ElapsedMilliseconds} ms.", DateTime.Now);
                            }

                            processingStopWatch.Reset();
                        }

                        stopwatch.Stop();
                        long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                        if (counter != 0)
                        {
                            _logger.LogInformation($"Completed proposal reminder task in {elapsedMilliseconds} ms. Reminded {counter} ({counter.ToWords()}) proposal submissions.", DateTime.Now);
                        }

                        else
                        {
                            _logger.LogInformation($"Completed proposal reminder task in {elapsedMilliseconds} ms. No proposal submissions to remind.", DateTime.Now);
                        }

                        stopwatch.Reset();
                        await Task.Delay(TimeSpan.FromMinutes(1).Subtract(TimeSpan.FromMilliseconds(elapsedMilliseconds)));
                    }
                }

                catch (Exception ex)
                {
                    var exception = ex;
                    while (exception is AggregateException)
                        exception = exception.InnerException;

                    _logger.LogCritical($"Proposal submission reminder task threw an exception: {exception.GetType()}: {exception.Message}.", DateTime.Now);

                    await errorLogsChannel.SendMessageAsync($"{ex.Message}");
                }
            });

            SharedData.IsProposalReminderInitialized = true;

            _logger.LogInformation("Initialized proposal submission reminder task.", DateTime.Now);
        }
    }
}
