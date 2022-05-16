using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using OSISDiscordAssistant.Utilities;

namespace OSISDiscordAssistant.Commands
{
    class HelpCommandsModule : BaseCommandModule
    {
        [Command("help")]
        public async Task HelpAsync(CommandContext ctx)
        {
            var embedBuilder = new DiscordEmbedBuilder() 
            {
                Title = "Listing All Commands...",
                Description = $"My prefixes are {ClientUtilities.GetPrefixList()}.",
                Timestamp = DateTime.Now,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "OSIS Discord Assistant"
                }
            };

            string councilCommands = $"{Formatter.Bold("event")} - Access the ARTEMIS (Automated Reminder & Event Management System or previously as Events Manager).\n" +
                $"{Formatter.Bold("remind")} - Reminder feature to remind yourself, another member, a division, or everyone.\n" +
                $"{Formatter.Bold("reminder")} - Commands to manage or view upcoming reminders.\n";

            string generalCommands = $"{Formatter.Bold("poll")} - Creates a poll with a set of specified emojis to choose.\n" +
                $"{Formatter.Bold("myinfo")} - Displays the info that this bot has over you.\n" +
                $"{Formatter.Bold("avatar")} - Shows a Discord profile picture of your account or another user.\n" +
                $"{Formatter.Bold("stats")} - Display statistics related to the bot.\n" +
                $"{Formatter.Bold("ping")} - Displays the WebSocket connection latency in milliseconds.\n" +
                $"{Formatter.Bold("prefix")} - Displays the bot's prefixes.\n" +
                $"{Formatter.Bold("about")} - Shows the bot's information.\n";

            string funCommands = $"{Formatter.Bold("slap")} - Slaps the tagged member or role.\n" +
                $"{Formatter.Bold("afk")} - Sets or removes your AFK status.\n" +
                $"{Formatter.Bold("flip")} - Flips a coin.\n" +
                $"{Formatter.Bold("snipe")} - Snipes a deleted message.\n" +
                $"{Formatter.Bold("snipeedit")} - Snipes the original content of an edited message.\n" +
                $"{Formatter.Bold("tags")} - Shows created tags.\n" +
                $"{Formatter.Bold("tag")} - Creates, updates, deletes, or mention a specified tag.\n";

            string verificationCommands = $"{Formatter.Bold("overify")} - Manually verifies a new member as a council member.\n";

            string administrationCommands = $"{Formatter.Bold("setname")} - Sets a new name for yourself or another user.\n" +
                $"{Formatter.Bold("giverole")} - Assigns a role to a member by specifying the role name.\n" +
                $"{Formatter.Bold("takerole")} - Removes a role from a member by specifying the role name.\n" +
                $"{Formatter.Bold("kick")} - Kicks a member from this Discord server.\n" +
                $"{Formatter.Bold("ban")} - Bans a member from this Discord server.\n" +
                $"{Formatter.Bold("unban")} - Unbans a member from this Discord server.\n" +
                $"{Formatter.Bold("mute")} - Mutes a member.\n" +
                $"{Formatter.Bold("unmute")} - Unmutes a member.\n" +
                $"{Formatter.Bold("prune")} - Prunes a channel from the specified message count to delete.\n" +
                $"{Formatter.Bold("lockdown")} - Locks down or unlocks a text channel for moderation purposes.\n" +
                $"{Formatter.Bold("announce")} - Sends an announcement message to a specified channel and role to mention.\n";

            string botAdminCommands = $"{Formatter.Bold("eval")} - Evaluates a snippet of C# code.\n" +
                $"{Formatter.Bold("kill")} - Disconnects the bot from Discord's gateway.\n";

            embedBuilder.AddField("Student Council Commands", councilCommands, false);
            embedBuilder.AddField("General Commands", generalCommands, false);
            embedBuilder.AddField("Fun Commands", funCommands, false);
            embedBuilder.AddField("Verification Commands", verificationCommands, false);
            embedBuilder.AddField("Administration Commands", administrationCommands, false);
            embedBuilder.AddField("Bot Administration Commands", botAdminCommands, false);
            embedBuilder.Color = DiscordColor.MidnightBlue;

            await ctx.Channel.SendMessageAsync(embed: embedBuilder);
        }
    }
}
