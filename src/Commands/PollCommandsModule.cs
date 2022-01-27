using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity.Enums;
using Humanizer;
using OSISDiscordAssistant.Models;

namespace OSISDiscordAssistant.Commands
{
    class PollCommandsModule : BaseCommandModule
    {
        [Command("poll")]
        public async Task PollAsync(CommandContext ctx, TimeSpan pollDuration, params DiscordEmoji[] emojiOptions)
        {
            if (emojiOptions.Count() < 2)
            {
                await ctx.RespondAsync($"{Formatter.Bold("[ERROR]")} To create a poll, there must be at least two emojis.");

                return;
            }

            int pollCounter = 0;

            using (var db = new CounterContext())
            {
                pollCounter = db.Counter.SingleOrDefault(x => x.Id == 1).PollCounter;               

                Counter rowToUpdate = null;
                rowToUpdate = db.Counter.SingleOrDefault(x => x.Id == 1);

                if (rowToUpdate != null)
                {
                    int incrementNumber = pollCounter + 1;
                    rowToUpdate.PollCounter = incrementNumber;
                }

                db.SaveChanges();
            }

            var pollEmbedBuilder = new DiscordEmbedBuilder
            {
                Title = $"Poll #{pollCounter}",
                Description = $"Click an emoji below to vote! This poll expires in {pollDuration.Humanize(2)} ({Formatter.Timestamp(pollDuration, TimestampFormat.LongDateTime)}).",
                Timestamp = DateTime.Now,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "OSIS Discord Assistant"
                }
            };

            var pollEmbed = await ctx.Channel.SendMessageAsync(embed: pollEmbedBuilder);

            Thread.Sleep(TimeSpan.FromSeconds(1));

            var interactivity = ctx.Client.GetInteractivity();

            var collectedEmojis = await interactivity.DoPollAsync(pollEmbed, emojiOptions, PollBehaviour.KeepEmojis, pollDuration);

            string resultEmojis = string.Empty;

            int voteCount = 0;

            foreach (var emoji in collectedEmojis)
            {
                if (emoji.Total > 1)
                {
                    resultEmojis = $"{resultEmojis}{emoji.Emoji}: {emoji.Total} ({emoji.Total.ToWords()}) votes.\n";
                }

                else
                {
                    resultEmojis = $"{resultEmojis}{emoji.Emoji}: {emoji.Total} ({emoji.Total.ToWords()}) vote.\n";
                }

                voteCount = voteCount + emoji.Total;
            }

            var pollResultEmbedBuilder = new DiscordEmbedBuilder
            {
                Title = $"Results for Poll #{pollCounter}",
                Timestamp = DateTime.Now,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "OSIS Discord Assistant"
                }
            };

            if (voteCount != 0)
            {
                string voteStatus = string.Empty;

                switch (voteCount)
                {
                    case > 1:
                        voteStatus = "votes";
                        break;
                    default:
                        voteStatus = "vote";
                        break;
                }

                pollResultEmbedBuilder.Description = $"A total of {voteCount} ({voteCount.ToWords()}) {voteStatus} have been collected.\n\n" +
                    $"{string.Join("\n", resultEmojis)}";
            }

            else
            {
                pollResultEmbedBuilder.Description = $"Nobody has casted their votes!\n\n" +
                    $"{string.Join("\n", resultEmojis)}";
            }

            await ctx.RespondAsync(embed: pollResultEmbedBuilder);
        }

        [Command("poll")]
        public async Task PollHelpAsync(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[SYNTAX]")} !poll [DURATION] [EMOJIS]\nExample: !poll 2h :rofl: :weary: :flag_us: :flag_cn:");
        }
    }
}
