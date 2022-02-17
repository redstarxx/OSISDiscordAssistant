using System;
using System.Diagnostics;
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
    public class VerificationCleanupService : IVerificationCleanupService
    {
        private readonly ILogger<VerificationCleanupService> _logger;
        private readonly VerificationContext _verificationContext;
        private readonly DiscordShardedClient _shardedClient;

        public VerificationCleanupService(ILogger<VerificationCleanupService> logger, VerificationContext verificationContext, DiscordShardedClient shardedClient)
        {
            _logger = logger;
            _verificationContext = verificationContext;
            _shardedClient = shardedClient;
        }

        public void Start()
        {
            if (SharedData.IsVerificationCleanupTaskInitialized)
            {
                _logger.LogInformation("Verification cleanup task is already fired. Skipping initialization.", DateTime.Now);

                return;
            }

            Task.Run(async () =>
            {
                DiscordChannel verificationProcessingChannel = await _shardedClient.GetShard(SharedData.MainGuildId).GetChannelAsync(SharedData.VerificationRequestsProcessingChannelId);

                Stopwatch stopwatch = new Stopwatch();

                int counter = 0;

                try
                {
                    while (true)
                    {
                        stopwatch.Start();

                        foreach (var row in _verificationContext.Verifications)
                        {
                            try
                            {
                                var requestEmbed = await verificationProcessingChannel.GetMessageAsync(row.VerificationEmbedId);

                                foreach (var embed in requestEmbed.Embeds.ToList())
                                {
                                    DateTimeOffset offset = (DateTimeOffset)embed.Timestamp;
                                    DateTime embedTimestamp = offset.DateTime;

                                    if (embedTimestamp.Subtract(TimeSpan.FromSeconds(DateTime.Now.Second)).AddDays(SharedData.MaxPendingVerificationWaitingDay) == DateTime.Now.Subtract(TimeSpan.FromSeconds(DateTime.Now.Second)) ||
                                    DateTime.Now.Subtract(TimeSpan.FromSeconds(DateTime.Now.Second)) > embedTimestamp.Subtract(TimeSpan.FromSeconds(DateTime.Now.Second)).AddDays(SharedData.MaxPendingVerificationWaitingDay))
                                    {
                                        counter++;

                                        DiscordEmbed updatedEmbed = null;

                                        DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder(embed)
                                        {
                                            Title = $"{embed.Title.Replace(" | PENDING", " | EXPIRED")}",
                                            Description = $"{embed.Description.Replace("PENDING.", $"EXPIRED (nobody handled this request, at {Formatter.Timestamp(DateTime.Now, TimestampFormat.LongDateTime)}).")}"
                                        };

                                        updatedEmbed = embedBuilder.Build();

                                        await requestEmbed.ModifyAsync(x => x.WithEmbed(updatedEmbed));

                                        await _shardedClient.GetShard(SharedData.MainGuildId).GetGuildAsync(SharedData.MainGuildId).Result
                                        .GetMemberAsync(row.UserId).Result.SendMessageAsync($"{Formatter.Bold("[VERIFICATION]")} I'm sorry, your verification request has expired (nobody responded to it within {SharedData.MaxPendingVerificationWaitingDay} ({SharedData.MaxPendingVerificationWaitingDay.ToWords()}) days). Feel free to try again or reach out to a member of Inti OSIS for assistance.");

                                        var request = _verificationContext.Verifications.SingleOrDefault(x => x.VerificationEmbedId == row.VerificationEmbedId);

                                        _verificationContext.Remove(request);
                                        await _verificationContext.SaveChangesAsync();

                                        _logger.LogInformation($"Removed verification request message ID {requestEmbed.Id}.", DateTime.Now);
                                    }
                                }
                            }

                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"An error occured while processing a verification request (ID: {row.Id}).");
                            }
                        }

                        stopwatch.Stop();

                        long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                        _logger.LogInformation($"Completed verification request cleanup task in {elapsedMilliseconds} ms. Removed {counter} ({counter.ToWords()}) requests.", DateTime.Now);

                        counter = 0;
                        stopwatch.Reset();
                        await Task.Delay(TimeSpan.FromMinutes(1).Subtract(TimeSpan.FromMilliseconds(elapsedMilliseconds)));
                    }
                }

                catch (Exception ex)
                {
                    var exception = ex;
                    while (exception is AggregateException)
                        exception = exception.InnerException;

                    _logger.LogCritical($"Verification cleanup task threw an exception: {exception.GetType()}: {exception.Message}.", DateTime.Now);
                }
            });

            SharedData.IsVerificationCleanupTaskInitialized = true;

            _logger.LogInformation("Initialized verification cleanup task.", DateTime.Now);
        }
    }
}
