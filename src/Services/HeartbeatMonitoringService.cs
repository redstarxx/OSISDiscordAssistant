using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using OSISDiscordAssistant.Constants;

namespace OSISDiscordAssistant.Services
{
    public class HeartbeatMonitoringService : IHeartbeatMonitoringService
    {
        private readonly ILogger<HeartbeatMonitoringService> _logger;
        private readonly DiscordShardedClient _shardedClient;

        public HeartbeatMonitoringService(ILogger<HeartbeatMonitoringService> logger, DiscordShardedClient shardedClient)
        {
            _logger = logger;
            _shardedClient = shardedClient;
        }

        public void Start()
        {
            if (SharedData.IsHeartbeatMonitoringTaskInitialized)
            {
                _logger.LogInformation("Heartbeat monitoring task is already fired. Skipping initialization.", DateTime.Now);

                return;
            }

            Task.Run(async () =>
            {
                while (true)
                {
                    DateTime lastMonitored = DateTime.Now;

                    await Task.Delay(TimeSpan.FromMinutes(5));

                    if (SharedData.ReceivedHeartbeats is 0)
                    {
                        _logger.LogCritical("No heartbeat has been received since {LastMonitored}. Terminating...", lastMonitored);

                        await _shardedClient.StopAsync();

                        Environment.Exit(0);
                    }

                    else
                    {
                        _logger.LogInformation("Received {ReceivedHeartbeats} heartbeats since {LastMonitored}. Resetting received heartbeats counter to 0.", SharedData.ReceivedHeartbeats, lastMonitored);

                        SharedData.ReceivedHeartbeats = 0;
                    }
                }
            });

            SharedData.IsHeartbeatMonitoringTaskInitialized = true;

            _logger.LogInformation("Initialized heartbeat monitoring task.", DateTime.Now);
        }
    }
}
