using System;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;
using Serilog.Templates;
using Serilog.Templates.Themes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OSISDiscordAssistant.Utilities;
using OSISDiscordAssistant.Services;
using OSISDiscordAssistant.Commands;
using OSISDiscordAssistant.Constants;
using OSISDiscordAssistant.Models;
using DSharpPlus;
using DSharpPlus.Interactivity;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity.Extensions;

namespace OSISDiscordAssistant
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                            .WriteTo.Logger(l => l.Filter.ByExcluding(c => c.Properties.Any(p => p.Value.ToString().Contains("Microsoft.EntityFrameworkCore")))
                            .Enrich.FromLogContext()
                            .WriteTo.Console(new ExpressionTemplate(Constant.LogConsoleFormat, theme: TemplateTheme.Code))
                            .WriteTo.File($@"{Environment.CurrentDirectory}/logs/clientlogs-.txt", LogEventLevel.Verbose, outputTemplate: Constant.LogFileFormat,
                            retainedFileCountLimit: null, rollingInterval: RollingInterval.Day, flushToDiskInterval: TimeSpan.FromMinutes(1)))
                            .CreateLogger();

            Log.Logger.Information("[1/9] Logging initialized.");

            Log.Logger.Information("OSISDiscordAssistant {Version}", ClientUtilities.GetBuildVersion());

            Log.Logger.Information("[2/9] Reading and loading config.json...");

            ClientUtilities.LoadConfigurationValues();

            Log.Logger.Information("[3/9] Initializing host builder...");

            void Builder(DbContextOptionsBuilder b)
            {
                b.UseNpgsql(SharedData.DbConnectionString, builder =>
                {
                    builder.EnableRetryOnFailure(10, TimeSpan.FromSeconds(30), null);
                });
            }

            var host = Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton(Log.Logger);

                    // Database
                    services.AddDbContext<VerificationContext>(Builder, ServiceLifetime.Scoped);
                    services.AddDbContext<EventContext>(Builder, ServiceLifetime.Scoped);
                    services.AddDbContext<TagsContext>(Builder, ServiceLifetime.Transient);
                    services.AddDbContext<CounterContext>(Builder, ServiceLifetime.Transient);
                    services.AddDbContext<ReminderContext>(Builder, ServiceLifetime.Transient);

                    // Services
                    services.AddSingleton<IMainGuildStaticInteractionHandler, MainGuildStaticInteractionHandler>();
                    services.AddSingleton<IEventReminderService, EventReminderService>();
                    services.AddSingleton<IProposalReminderService, ProposalReminderService>();
                    services.AddSingleton<IVerificationCleanupService, VerificationCleanupService>();
                    services.AddSingleton<IStatusUpdaterService, StatusUpdaterService>();
                    services.AddSingleton<IHeartbeatMonitoringService, HeartbeatMonitoringService>();

                    services.AddSingleton<IReminderService, ReminderService>();

                    // Client
                    services.AddSingleton<DiscordShardedClient>(s => new(new DiscordConfiguration
                    {
                        Token = SharedData.Token,
                        TokenType = TokenType.Bot,
                        AutoReconnect = true,
                        MinimumLogLevel = LogLevel.Information,
                        Intents = DiscordIntents.All,
                        LoggerFactory = s.GetService<ILoggerFactory>()
                    }));

                    // Event handlers
                    services.AddSingleton<EventHandlers>();
                })
                .UseConsoleLifetime()
                .Build();

            var eventHandlers = host.Services.GetRequiredService<EventHandlers>();

            var shardedClient = host.Services.GetRequiredService<DiscordShardedClient>();

            Log.Logger.Information("[4/9] Registering client event handlers...");

            shardedClient.Ready += eventHandlers.OnClientReady;
            shardedClient.GuildDownloadCompleted += eventHandlers.OnGuildDownloadCompleted;
            shardedClient.GuildMemberAdded += eventHandlers.OnGuildMemberAdded;
            shardedClient.GuildMemberRemoved += eventHandlers.OnGuildMemberRemoved;
            shardedClient.GuildAvailable += eventHandlers.OnGuildAvailable;
            shardedClient.GuildCreated += eventHandlers.OnGuildCreated;
            shardedClient.GuildDeleted += eventHandlers.OnGuildDeleted;
            shardedClient.MessageCreated += eventHandlers.OnMessageCreated;
            shardedClient.MessageUpdated += eventHandlers.OnMessageUpdated;
            shardedClient.MessageDeleted += eventHandlers.OnMessageDeleted;
            shardedClient.MessageReactionAdded += eventHandlers.OnMessageReactionAdded;
            shardedClient.MessageReactionRemoved += eventHandlers.OnMessageReactionRemoved;
            shardedClient.ComponentInteractionCreated += eventHandlers.OnComponentInteractionCreated;
            shardedClient.SocketErrored += eventHandlers.OnSocketErrored;
            shardedClient.ClientErrored += eventHandlers.OnClientErrored;
            shardedClient.Heartbeated += eventHandlers.OnHeartbeated;
            shardedClient.UnknownEvent += eventHandlers.OnUnknownEvent;

            Log.Logger.Information("[5/9] Loading up interactivity configuration...");
            await shardedClient.UseInteractivityAsync(new InteractivityConfiguration
            {
                Timeout = TimeSpan.FromDays(7),
                AckPaginationButtons = true,
            });

            Log.Logger.Information("[6/9] Loading up CommandsNext configuration...");
            var commandsConfig = new CommandsNextConfiguration
            {
                StringPrefixes = SharedData.Prefixes,
                EnableMentionPrefix = true,
                EnableDms = false,
                EnableDefaultHelp = false,
                Services = host.Services
            };

            SharedData.Commands = await shardedClient.UseCommandsNextAsync(commandsConfig);

            Log.Logger.Information("[7/9] Registering CommandsNext commands modules...");
            // Registers commands.
            foreach (var cmd in SharedData.Commands.Values)
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
                cmd.RegisterCommands<COVIDStatisticsCommandsModule>();
            }

            Log.Logger.Information("[8/9] Registering CommandsNext commands event handlers...");

            // Registers event handlers related to CommandsNext.
            foreach (var hndlr in SharedData.Commands.Values)
            {
                hndlr.CommandExecuted += eventHandlers.CommandsNext_CommandExecuted;
                hndlr.CommandErrored += eventHandlers.CommandsNext_CommandErrored;
            }

            // Tell that whoever is seeing this that the client is connecting to Discord's gateway.
            Log.Logger.Information("[9/9] Initializing and connecting all shards...\n----------------------------------------");

            await shardedClient.StartAsync();

            await Task.Delay(-1);
        }
    }
}
