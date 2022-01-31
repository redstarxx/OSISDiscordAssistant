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
        public async Task CreateUpdateDeleteTagsAsync(CommandContext ctx, string operationSelection, string tagName, params string[] tagContent)
        {
            try
            {
                if (operationSelection == "create")
                {
                    bool isExist = _tagsContext.Tags.Any(x => x.Name == tagName);
                    string tagContentToWrite = string.Join(" ", tagContent);

                    if (isExist)
                    {
                        await ctx.RespondAsync($"The tag {Formatter.InlineCode(tagName)} already exists!");

                        return;
                    }

                    if (tagContentToWrite.Length == 0)
                    {
                        await ctx.RespondAsync($"{Formatter.Bold("[ERROR]")} Tag content cannot be left empty!");

                        return;
                    }

                    _tagsContext.Add(new Tags
                    {
                        Name = tagName,
                        Content = tagContentToWrite
                    });

                    _tagsContext.SaveChanges();

                    await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"));
                }

                else if (operationSelection == "update" || operationSelection == "edit")
                {
                    string tagContentToWrite = string.Join(" ", tagContent);

                    if (tagContentToWrite.Length == 0)
                    {
                        await ctx.RespondAsync($"{Formatter.Bold("[ERROR]")} Tag content cannot be left empty!");

                        return;
                    }

                    Tags tagToUpdate = null;
                    tagToUpdate = _tagsContext.Tags.SingleOrDefault(x => x.Name == tagName);

                    tagToUpdate.Content = string.Join(" ", tagContent);

                    _tagsContext.SaveChanges();

                    await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"));
                }

                else if (operationSelection == "delete")
                {
                    Tags tagToDelete = null;
                    tagToDelete = _tagsContext.Tags.SingleOrDefault(x => x.Name == tagName);

                    _tagsContext.Remove(tagToDelete);

                    _tagsContext.SaveChanges();

                    await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"));
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

            catch (Exception ex)
            {
                await ctx.RespondAsync($"{Formatter.Bold("[ERROR]")} An error occurred. Did you tried to delete a nonexistent tag?\nError details: {Formatter.InlineCode($"{ex.Message.GetType()}: {ex.Message}")}");
            }
        }

        /// <summary>
        /// Retrieves all names of created tags.
        /// </summary>
        /// <returns>A <see cref="List{T}" /> of tag names.</returns>
        internal List<string> GetAllTags()
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
            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[SYNTAX]")} !tag [CREATE/UPDATE/EDIT/DELETE] [TAGNAME] [TAGCONTENT]");
        }
    }
}
