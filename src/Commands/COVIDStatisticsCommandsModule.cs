using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using KawalCoronaSharp;
using KawalCoronaSharp.Enums;
using OSISDiscordAssistant.Utilities;

namespace OSISDiscordAssistant.Commands
{
    class COVIDStatisticsCommandsModule : BaseCommandModule
    {
        [Command("covid")]
        public async Task CovidStatisticAsync(CommandContext ctx, [RemainingText] string countryName)
        {
            await ctx.TriggerTypingAsync();

            KawalCoronaApi api = new KawalCoronaApi();

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
            {
                Description = $"To access another country or global statistic, type {Formatter.InlineCode("osis covid global")} or {Formatter.InlineCode("osis covid [COUNTRY / REGION]")}. Note that the given numbers are accumulated since the beginning of the pandemic.",
                Color = DiscordColor.DarkBlue,
                Timestamp = DateTime.Now,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "OSIS Discord Assistant"
                },
            };

            if (countryName is null)
            {
                embedBuilder.WithTitle($"COVID-19 Statistics for Indonesia {DiscordEmoji.FromName(ctx.Client, ":flag_id:")}");

                var response = await api.GetIndonesianDataAsync();

                var lastUpdatedResponse = await api.GetCountryDataAsync("Indonesia", SearchMode.Exact);

                embedBuilder.WithDescription($"{embedBuilder.Description}\nLast updated: {Formatter.Timestamp(ClientUtilities.ConvertUnixTimestampToDateTime(lastUpdatedResponse.LastUpdated), TimestampFormat.LongDateTime)}");

                embedBuilder.AddField("Confirmed", response.Positives, true)
                            .AddField("Recovered", response.Recovered, true)
                            .AddField("Deaths", response.Deceased, true)
                            .AddField("Hospitalised", response.Hospitalised, true);
            }

            else if (countryName is "global")
            {
                embedBuilder.WithTitle($"Global COVID-19 Statistics {DiscordEmoji.FromName(ctx.Client, ":globe_with_meridians:")}");

                var positive = await api.GetPartialGlobalDataAsync(DataType.Positive);
                var recovered = await api.GetPartialGlobalDataAsync(DataType.Recovered);
                var deaths = await api.GetPartialGlobalDataAsync(DataType.Deaths);

                embedBuilder.AddField("Confirmed", positive.Value, true)
                            .AddField("Recovered", recovered.Value, true)
                            .AddField("Deaths", deaths.Value, true);
            }

            else
            {
                var response = await api.GetCountryDataAsync(countryName, SearchMode.ClosestMatching);

                embedBuilder.WithTitle($"COVID-19 Statistics for {response.Country}");

                embedBuilder.WithDescription($"{embedBuilder.Description}\nLast updated: {Formatter.Timestamp(ClientUtilities.ConvertUnixTimestampToDateTime(response.LastUpdated), TimestampFormat.LongDateTime)}");

                embedBuilder.AddField("Active Cases", response.Active.ToString("N0"), true)
                            .AddField("Confirmed", response.Confirmed.ToString("N0"), true)
                            .AddField("Recovered", response.Recovered.ToString("N0"), true)
                            .AddField("Deaths", response.Deaths.ToString("N0"), true);
            }

            await ctx.RespondAsync(embedBuilder.Build());
        }
    }
}
