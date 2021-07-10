using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Humanizer;

namespace discordbot.Commands
{
    class PollCommandsModule : BaseCommandModule
    {
        [Command("poll")]
        public async Task PollAsync(CommandContext ctx, TimeSpan pollDuration, params DiscordEmoji[] emojiOptions)
        {
            using (var db = new PollCounterContext())
            {
                int counter = db.PollCounter.SingleOrDefault(x => x.Id == 1).Counter;

                var pollEmbedBuilder = new DiscordEmbedBuilder
                {
                    Title = $"Poll #{counter}",
                    Description = $"Click an emoji below to vote! This poll expires in {pollDuration.Humanize(2)}.",
                    Timestamp = ClientUtilities.GetWesternIndonesianDateTime(),
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = "OSIS Discord Assistant"
                    }
                };

                var pollEmbed = await ctx.Channel.SendMessageAsync(embed: pollEmbedBuilder).ConfigureAwait(false);

                foreach (var option in emojiOptions)
                {
                    await pollEmbed.CreateReactionAsync(option).ConfigureAwait(false);
                }

                Thread.Sleep(TimeSpan.FromSeconds(1));

                var interactivity = ctx.Client.GetInteractivity();

                var collectedEmojis = await interactivity.CollectReactionsAsync(pollEmbed, pollDuration).ConfigureAwait(false);
                var distinctResult = collectedEmojis.Distinct();

                var resultEmojis = distinctResult.Select(x => $"{x.Emoji}: {x.Total} ({x.Total.ToWords()}) voter(s).");

                var pollResultEmbedBuilder = new DiscordEmbedBuilder
                {
                    Title = $"Results for Poll #{counter}",
                    Timestamp = ClientUtilities.GetWesternIndonesianDateTime(),
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = "OSIS Discord Assistant"
                    }
                };

                if (collectedEmojis.Count() != 0)
                {
                    pollResultEmbedBuilder.Description = $"A total of {collectedEmojis.Count()} ({collectedEmojis.Count().ToWords()}) votes have been collected.\n\n" +
                        $"{string.Join("\n", resultEmojis)}";
                }

                else
                {
                    pollResultEmbedBuilder.Description = "Nobody has casted their votes!";
                }

                await ctx.Channel.SendMessageAsync(embed: pollResultEmbedBuilder).ConfigureAwait(false);

                PollCounter rowToUpdate = null;
                rowToUpdate = db.PollCounter.SingleOrDefault(x => x.Id == 1);

                if (rowToUpdate != null)
                {
                    int incrementNumber = counter + 1;
                    rowToUpdate.Counter = incrementNumber;
                }

                db.SaveChanges();
            }            
        }
    }
}
