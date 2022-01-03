using System;
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
        /// <summary>
        /// Gets all stored tag names.
        /// </summary>
        [Command("tags")]
        public async Task ShowAllTagsAsync(CommandContext ctx)
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("List of all created tags:\n\n");

            var tags = await GetAllTagsAsync();
            stringBuilder.Append(tags);

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

            using (var db = new TagsContext())
            {
                try
                {
                    var tag = db.Tags.SingleOrDefault(x => x.Name == tagName);

                    if (tag is null)
                    {
                        throw new Exception();
                    }

                    tagContent.Append(tag.Content);
                }

                catch
                {
                    tagContent.Append("Specified tag was not found. Here are some suggestions:\n\n");

                    var allTags = await GetAllTagsAsync();
                    tagContent.Append(allTags);
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
                    using (var db = new TagsContext())
                    {
                        bool isExist = db.Tags.Any(x => x.Name == tagName);
                        string tagContentToWrite = string.Join(" ", tagContent);

                        if (isExist)
                        {
                            string toSend = $"The tag {Formatter.InlineCode(tagName)} already exists!";
                            await ctx.RespondAsync(toSend);

                            return;
                        }

                        if (tagContentToWrite.Length == 0)
                        {
                            string toSend = $"{Formatter.Bold("[ERROR]")} Tag content cannot be left empty!";
                            await ctx.RespondAsync(toSend);

                            return;
                        }

                        db.Add(new Tags
                        {
                            Name = tagName,
                            Content = tagContentToWrite
                        });

                        db.SaveChanges();

                        var thumbsUpEmoji = DiscordEmoji.FromName(ctx.Client, ":thumbsup:");

                        await ctx.RespondAsync(thumbsUpEmoji);
                    }
                }

                else if (operationSelection == "update" || operationSelection == "edit")
                {
                    using (var db = new TagsContext())
                    {
                        string tagContentToWrite = string.Join(" ", tagContent);

                        if (tagContentToWrite.Length == 0)
                        {
                            string toSend = $"{Formatter.Bold("[ERROR]")} Tag content cannot be left empty!";
                            await ctx.RespondAsync(toSend);

                            return;
                        }

                        Tags tagToUpdate = null;
                        tagToUpdate = db.Tags.SingleOrDefault(x => x.Name == tagName);

                        tagToUpdate.Content = string.Join(" ", tagContent);

                        db.SaveChanges();

                        var thumbsUpEmoji = DiscordEmoji.FromName(ctx.Client, ":thumbsup:");

                        await ctx.RespondAsync(thumbsUpEmoji);
                    }
                }

                else if (operationSelection == "delete")
                {
                    using (var db = new TagsContext())
                    {
                        Tags tagToDelete = null;
                        tagToDelete = db.Tags.SingleOrDefault(x => x.Name == tagName);

                        db.Remove(tagToDelete);

                        db.SaveChanges();

                        var thumbsUpEmoji = DiscordEmoji.FromName(ctx.Client, ":thumbsup:");

                        await ctx.RespondAsync(thumbsUpEmoji);
                    }
                }

                else
                {
                    var helpEmoji = DiscordEmoji.FromName(ctx.Client, ":sos:");
                    string toSend = $"{Formatter.Bold("[ERROR]")} The parameter {Formatter.InlineCode(operationSelection)} is invalid. Type {Formatter.InlineCode("!tag")} to list all options. Alternatively, click the emoji below to get help.";

                    var errorMessage = await ctx.Channel.SendMessageAsync(toSend);

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
                string toSend = $"{Formatter.Bold("[ERROR]")} An error occurred. Did you tried to delete a nonexistent tag?\nError details: {Formatter.InlineCode($"{ex.Message.GetType()}: {ex.Message}")}";
                await ctx.RespondAsync(toSend);
            }
        }

        /// <summary>
        /// Retrieves all names of created tags.
        /// </summary>
        /// <returns>The list of tag names.</returns>
        internal async Task<string> GetAllTagsAsync()
        {
            StringBuilder tags = new StringBuilder();
            int counter = 0;

            using (var db = new TagsContext())
            {
                foreach (var tag in db.Tags)
                {
                    if (counter == 0)
                    {
                        tags.Append($"{Formatter.InlineCode(tag.Name)}");
                    }

                    else
                    {
                        tags.Append($", {Formatter.InlineCode(tag.Name)}");
                    }

                    counter++;
                }

                if (counter == 0)
                {
                    tags.Append("There are no tags to show!");
                }
            }

            return tags.ToString();
        }

        // ----------------------------------------------------------
        // COMMAND HELPERS BELOW
        // ----------------------------------------------------------

        [Command("tag")]
        public async Task TagHelpAsync(CommandContext ctx)
        {
            string toSend = $"{Formatter.Bold("[SYNTAX]")} !tag [CREATE/UPDATE/EDIT/DELETE] [TAGNAME] [TAGCONTENT]";

            await ctx.Channel.SendMessageAsync(toSend);
        }
    }
}
