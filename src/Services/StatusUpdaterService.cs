﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using OSISDiscordAssistant.Constants;

namespace OSISDiscordAssistant.Services
{
    public class StatusUpdaterService : IStatusUpdaterService
    {
        private readonly ILogger<StatusUpdaterService> _logger;
        private readonly DiscordShardedClient _shardedClient;

        private bool initialized = false;

        public StatusUpdaterService(ILogger<StatusUpdaterService> logger, DiscordShardedClient shardedClient)
        {
            _logger = logger;
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
                Stopwatch stopwatch = new Stopwatch();

                int index = 0;

                string currentCustomStatus = null;

                List<string> customStatusList = new List<string>();

                foreach (string customStatus in SharedData.CustomStatusDisplay)
                {
                    customStatusList.Add(customStatus);
                }

                ActivityType activityType = new ActivityType();

                switch (SharedData.StatusActivityType)
                {
                    case 0:
                        activityType = ActivityType.Playing;
                        break;
                    case 1:
                        activityType = ActivityType.Streaming;
                        break;
                    case 2:
                        activityType = ActivityType.ListeningTo;
                        break;
                    case 3:
                        activityType = ActivityType.Watching;
                        break;
                    case 4:
                        activityType = ActivityType.Custom;
                        break;
                    case 5:
                        activityType = ActivityType.Competing;
                        break;
                    default:
                        break;
                }

                while (true)
                {
                    stopwatch.Start();

                    try
                    {
                        currentCustomStatus = customStatusList[index];

                        index++;
                    }

                    // Index out of range? Handle it down here.
                    catch
                    {
                        index = 0;
                        currentCustomStatus = customStatusList[index];

                        index++;
                    }

                    var activity = new DiscordActivity(currentCustomStatus, activityType);
                    await _shardedClient.UpdateStatusAsync(activity);

                    stopwatch.Stop();
                    long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                    stopwatch.Reset();

                    _logger.LogInformation("Presence updated: '{ActivityType} {ActivityName}' in {ElapsedMilliseconds} ms.", activity.ActivityType, activity.Name, elapsedMilliseconds);

                    await Task.Delay(TimeSpan.FromMinutes(2).Subtract(TimeSpan.FromMilliseconds(elapsedMilliseconds)));
                }
            });

            initialized = true;

            _logger.LogInformation("Initialized status updater task.");
        }
    }
}
