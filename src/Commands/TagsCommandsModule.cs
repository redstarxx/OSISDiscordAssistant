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
using OSISDiscordAssistant.Models;
using OSISDiscordAssistant.Utilities;

namespace OSISDiscordAssistant.Commands
{
    class TagsCommandsModule : BaseCommandModule
    {
        private readonly TagsContext _tagsContext;

        public TagsCommandsModule(TagsContext tagsContext)
        {
            _tagsContext = tagsContext;
        }

        /// <summary>
        /// Gets all stored tag names.
        /// </summary>
        [Command("tags")]
        public async Task ShowAllTagsAsync(CommandContext ctx)
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("List of all created tags:\n\n");

            var tags = GetAllTags();

            foreach (string tag in tags.ToList())
            {
                stringBuilder.Append(tag);
            }

            await ctx.Channel.SendMessageAsync(stringBuilder.ToString());
        }

        /// <summary>
        /// Get a specified tag content via the given tag name.
        /// </summary>
        /// <param name="tagName">The tag name to get its content.</param>
        [Command("tag")]
        public async Task ShowTagsAsync(CommandContext ctx, string tagName)
        {
            StringBuilder tagContent = new StringBuilder();

            try
            {
                var tag = _tagsContext.Tags.SingleOrDefault(x => x.Name == tagName);

                if (tag is null)
                {
                    throw new Exception();
                }

                tagContent.Append(tag.Content);
            }

            catch
            {
                tagContent.Append("Specified tag was not found. Here are some suggestions:\n\n");

                var allTagNames = GetAllTags();

                foreach (string tag in allTagNames.ToList())
                {
                    if (!tag.Contains(tagName))
                    {
                        allTagNames.RemoveAll(x => x == tag);
                    }

                    else
                    {
                        tagContent.Append(tag);
                    }
                }
            }

            if (ctx.Message.ReferencedMessage is not null)
            {
                await ctx.Message.ReferencedMessage.RespondAsync(tagContent.ToString());
            }

            else
            {
                await ctx.Channel.SendMessageAsync(tagContent.ToString());
            }
        }

        /// <summary>
        /// Create, update, or delete a specified tag.
        /// </summary>
        /// <param name="operationSelection">Create, update, or delete a tag.</param>
        /// <param name="tagName">The tag name to create, update, or delete.</param>
        /// <param name="tagContent">The tag content to create or update.</param>
        /// <returns></returns>
        [Command("tag")]
        public async Task CreateUpdateDeleteTagsAsync(CommandContext ctx, string operationSelection, string tagName, [RemainingText] string tagContent)
        {
            if (operationSelection == "create")
            {
                if (_tagsContext.Tags.Any(x => x.Name == tagName))
                {
                    await ctx.RespondAsync($"The tag {Formatter.InlineCode(tagName)} already exists!");

                    return;
                }

                if (tagContent.Length == 0)
                {
                    await ctx.RespondAsync($"{Formatter.Bold("[ERROR]")} Tag content cannot be left empty!");

                    return;
                }

                _tagsContext.Add(new Tags
                {
                    Name = tagName,
                    Content = tagContent,
                    CreatorUserId = ctx.Member.Id,
                    CreatedTimestamp = ClientUtilities.GetCurrentUnixTimestamp(),
                    VersionCount = 1
                });

                _tagsContext.SaveChanges();

                await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"));
            }

            else if (operationSelection == "update" || operationSelection == "edit")
            {
                if (tagContent.Length == 0)
                {
                    await ctx.RespondAsync($"{Formatter.Bold("[ERROR]")} Tag content cannot be left empty!");

                    return;
                }

                Tags tagToUpdate = _tagsContext.Tags.SingleOrDefault(x => x.Name == tagName);

                if (tagToUpdate is null)
                {
                    await ctx.RespondAsync($"{Formatter.Bold("[ERROR]")} Tag {Formatter.InlineCode(tagName)} does not exist.");

                    return;
                }

                tagToUpdate.Content = tagContent;
                tagToUpdate.UpdaterUserId = ctx.Member.Id;
                tagToUpdate.LastUpdatedTimestamp = ClientUtilities.GetCurrentUnixTimestamp();
                tagToUpdate.VersionCount = tagToUpdate.VersionCount + 1;

                _tagsContext.SaveChanges();

                await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"));
            }

            else if (operationSelection == "delete")
            {
                Tags tag = _tagsContext.Tags.SingleOrDefault(x => x.Name == tagName);

                if (tag is null)
                {
                    await ctx.RespondAsync($"{Formatter.Bold("[ERROR]")} Tag {Formatter.InlineCode(tagName)} does not exist.");

                    return;
                }

                _tagsContext.Remove(tag);

                _tagsContext.SaveChanges();

                await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"));
            }

            else if (operationSelection == "info")
            {
                var tag = _tagsContext.Tags.SingleOrDefault(x => x.Name == tagName);

                if (tag is null)
                {
                    await ctx.RespondAsync($"{Formatter.Bold("[ERROR]")} Tag {Formatter.InlineCode(tagName)} does not exist.");

                    return;
                }

                var creator = await ctx.Client.GetUserAsync(tag.CreatorUserId);

                DiscordEmbedBuilder embedBuilder = new()
                {
                    Author = new DiscordEmbedBuilder.EmbedAuthor
                    {
                        Name = $"{creator.Username}#{creator.Discriminator}",
                        IconUrl = creator.AvatarUrl
                    },
                    Title = tag.Name
                };

                var lastUpdatedTimestamp = "N/A";

                if (tag.LastUpdatedTimestamp != null)
                {
                    DiscordUser lastUpdatingUser = await ctx.Client.GetUserAsync((ulong)tag.UpdaterUserId);

                    lastUpdatedTimestamp = $"{Formatter.Timestamp(ClientUtilities.ConvertUnixTimestampToDateTime((long)tag.LastUpdatedTimestamp), TimestampFormat.LongDateTime)} by {lastUpdatingUser.Mention}";
                }

                embedBuilder.AddField("Created at", Formatter.Timestamp(ClientUtilities.ConvertUnixTimestampToDateTime(tag.CreatedTimestamp), TimestampFormat.LongDateTime))
                            .AddField("Last updated at", lastUpdatedTimestamp)
                            .AddField("Version count", tag.VersionCount.ToString(), true);

                await ctx.RespondAsync(embedBuilder.Build());
            }

            else
            {
                var helpEmoji = DiscordEmoji.FromName(ctx.Client, ":sos:");

                var errorMessage = await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} The parameter {Formatter.InlineCode(operationSelection)} is invalid. Type {Formatter.InlineCode("!tag")} to list all options. Alternatively, click the emoji below to get help.");

                await errorMessage.CreateReactionAsync(helpEmoji);

                var interactivity = ctx.Client.GetInteractivity();

                Thread.Sleep(TimeSpan.FromMilliseconds(500));
                var emojiResult = await interactivity.WaitForReactionAsync(x => x.Message == errorMessage && (x.Emoji == helpEmoji));

                if (emojiResult.Result.Emoji == helpEmoji)
                {
                    string helpMessage = $"{Formatter.Bold("[SYNTAX]")} !tag [CREATE/UPDATE/EDIT/DELETE] [TAGNAME] [TAGCONTENT]";

                    await ctx.Channel.SendMessageAsync(helpMessage);
                }
            }
        }

        /// <summary>
        /// Retrieves all names of created tags.
        /// </summary>
        /// <returns>A <see cref="List{T}" /> of tag names.</returns>
        private List<string> GetAllTags()
        {
            List<string> tags = new List<string>();
            int counter = 0;

            foreach (var tag in _tagsContext.Tags)
            {
                if (counter == 0)
                {
                    tags.Add($"{Formatter.InlineCode(tag.Name)}");
                }

                else
                {
                    tags.Add($", {Formatter.InlineCode(tag.Name)}");
                }

                counter++;
            }

            if (counter == 0)
            {
                tags.Add("There are no tags to show!");
            }

            return tags;
        }

        // ----------------------------------------------------------
        // COMMAND HELPERS BELOW
        // ----------------------------------------------------------

        [Command("tag")]
        public async Task TagHelpAsync(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[SYNTAX]")} osis tag [CREATE/UPDATE/EDIT/DELETE/INFO] [TAGNAME] [TAGCONTENT]\nExample: {Formatter.InlineCode("osis tag create pendaftaran Untuk mendaftar, silahkan kunjungi link berikut.")}");
        }
    }
}
