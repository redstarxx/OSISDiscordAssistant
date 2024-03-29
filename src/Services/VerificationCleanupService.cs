﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Humanizer;
using Microsoft.Extensions.Logging;
using OSISDiscordAssistant.Models;

namespace OSISDiscordAssistant.Services
{
    public class VerificationCleanupService : IVerificationCleanupService
    {
        private readonly ILogger<VerificationCleanupService> _logger;
        private readonly VerificationContext _verificationContext;
        private readonly DiscordShardedClient _shardedClient;

        private bool initialized = false;

        public VerificationCleanupService(ILogger<VerificationCleanupService> logger, VerificationContext verificationContext, DiscordShardedClient shardedClient)
        {
            _logger = logger;
            _verificationContext = verificationContext;
            _shardedClient = shardedClient;
        }

        public void Start()
        {
            if (initialized)
            {
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
                                        .GetMemberAsync(row.UserId).Result.SendMessageAsync($"{Formatter.Bold("[VERIFICATION]")} Mohon maaf, permintaan verifikasimu telah expired (tidak ada yang menanggapi dalam {SharedData.MaxPendingVerificationWaitingDay} ({SharedData.MaxPendingVerificationWaitingDay.ToWords()}) hari). Silahkan DM salah satu anggota Inti OSIS untuk bantuan.");

                                        var request = _verificationContext.Verifications.SingleOrDefault(x => x.VerificationEmbedId == row.VerificationEmbedId);

                                        _verificationContext.Remove(request);
                                        await _verificationContext.SaveChangesAsync();

                                        _logger.LogInformation("Removed verification request message ID {Id}.", requestEmbed.Id);
                                    }
                                }
                            }

                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "An error occured while processing a verification request (ID: {Id}).", row.Id);
                            }
                        }

                        stopwatch.Stop();

                        long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                        _logger.LogInformation("Removed {Counter} ({CountWords}) expired verification requests in {ElapsedMilliseconds} ms.", counter, counter.ToWords(), elapsedMilliseconds);

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

                    _logger.LogCritical("Verification cleanup task threw an exception: {ExceptionType}: {ExceptionMessage}.", exception.GetType(), exception.Message);
                }
            });

            initialized = true;

            _logger.LogInformation("Initialized verification cleanup task.");
        }
    }
}
