using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace discordbot.Commands
{
    class TagsCommandsModule : BaseCommandModule
    {
        /// <summary>
        /// Gets all stored tag names.
        /// </summary>
        [Command("tags")]
        public async Task GetAllTagsAsync(CommandContext ctx)
        {
            int counter = 0;
            StringBuilder toSend = new StringBuilder();

            using (var db = new TagsContext())
            {
                toSend.Append("Following tags matching your query were found:\n\n");

                foreach (var tag in db.Tags)
                {
                    if (counter == 0)
                    {
                        toSend.Append($"{Formatter.InlineCode(tag.TagName)}");
                    }

                    else
                    {
                        toSend.Append($", {Formatter.InlineCode(tag.TagName)}");
                    }

                    counter++;
                }

                if (counter == 0)
                {
                    toSend.Append("There are no tags to show!");
                }
            }

            await ctx.Channel.SendMessageAsync(toSend.ToString()).ConfigureAwait(false);
        }

        /// <summary>
        /// Get a specified tag content via the given tag name.
        /// </summary>
        /// <param name="tagName">The tag name to get its content.</param>
        [Command("tag")]
        public async Task ShowTagsAsync(CommandContext ctx, string tagName)
        {
            StringBuilder tagContent = new StringBuilder();
            int counter = 0;

            using (var db = new TagsContext())
            {
                try
                {
                    tagContent.Append(db.Tags.SingleOrDefault(x => x.TagName == tagName).TagContent);
                }

                catch
                {
                    tagContent.Append("Specified tag was not found. Here are some suggestions:\n\n");

                    foreach (var tag in db.Tags)
                    {

                        if (tag.TagName.Contains(tagName))
                        {
                            if (counter == 0)
                            {
                                tagContent.Append($"{Formatter.InlineCode(tag.TagName)}");
                            }

                            else
                            {
                                tagContent.Append($", {Formatter.InlineCode(tag.TagName)}");
                            }

                            counter++;
                        }
                    }

                    if (counter == 0)
                    {
                        tagContent.Clear();

                        tagContent.Append($"The tag {Formatter.InlineCode(tagName)} does not exist!");
                    }
                }
            }

            await ctx.Channel.SendMessageAsync(tagContent.ToString()).ConfigureAwait(false);
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
                        bool isExist = db.Tags.Any(x => x.TagName == tagName);
                        string tagContentToWrite = string.Join(" ", tagContent);

                        if (isExist)
                        {
                            string toSend = $"The tag {Formatter.InlineCode(tagName)} already exists!";
                            await ctx.RespondAsync(toSend).ConfigureAwait(false);

                            return;
                        }

                        if (tagContentToWrite.Length == 0)
                        {
                            string toSend = $"{Formatter.Bold("[ERROR]")} Tag content cannot be left empty!";
                            await ctx.RespondAsync(toSend).ConfigureAwait(false);

                            return;
                        }

                        db.Add(new Tags
                        {
                            TagName = tagName,
                            TagContent = tagContentToWrite
                        });

                        db.SaveChanges();

                        var thumbsUpEmoji = DiscordEmoji.FromName(ctx.Client, ":thumbsup:");

                        await ctx.RespondAsync(thumbsUpEmoji).ConfigureAwait(false);
                    }
                }

                else if (operationSelection == "update")
                {
                    using (var db = new TagsContext())
                    {
                        string tagContentToWrite = string.Join(" ", tagContent);

                        if (tagContentToWrite.Length == 0)
                        {
                            string toSend = $"{Formatter.Bold("[ERROR]")} Tag content cannot be left empty!";
                            await ctx.RespondAsync(toSend).ConfigureAwait(false);

                            return;
                        }

                        Tags tagToUpdate = null;
                        tagToUpdate = db.Tags.SingleOrDefault(x => x.TagName == tagName);

                        tagToUpdate.TagContent = string.Join(" ", tagContent);

                        db.SaveChanges();

                        var thumbsUpEmoji = DiscordEmoji.FromName(ctx.Client, ":thumbsup:");

                        await ctx.RespondAsync(thumbsUpEmoji).ConfigureAwait(false);
                    }
                }

                else if (operationSelection == "delete")
                {
                    using (var db = new TagsContext())
                    {
                        Tags tagToDelete = null;
                        tagToDelete = db.Tags.SingleOrDefault(x => x.TagName == tagName);

                        db.Remove(tagToDelete);

                        db.SaveChanges();

                        var thumbsUpEmoji = DiscordEmoji.FromName(ctx.Client, ":thumbsup:");

                        await ctx.RespondAsync(thumbsUpEmoji).ConfigureAwait(false);
                    }
                }
            }

            catch (Exception ex)
            {
                string toSend = $"{Formatter.Bold("[ERROR]")} An error occurred. Did you tried to delete a nonexistent tag?\nError details: {Formatter.InlineCode($"{ex.Message.GetType()}: {ex.Message}")}";
                await ctx.RespondAsync(toSend).ConfigureAwait(false);
            }
        }

        // ----------------------------------------------------------
        // COMMAND HELPERS BELOW
        // ----------------------------------------------------------

        [Command("tag")]
        public async Task TagHelpAsync(CommandContext ctx)
        {
            string toSend = $"{Formatter.Bold("[SYNTAX]")} !tag [CREATE/UPDATE/DELETE] [TAGNAME] [TAGCONTENT]";

            await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
        }
    }
}
