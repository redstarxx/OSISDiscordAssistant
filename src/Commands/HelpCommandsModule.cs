using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace discordbot.Commands
{
    class HelpCommandsModule : BaseCommandModule
    {
        [Command("help")]
        public async Task Help(CommandContext ctx)
        {
            var embedBuilder = new DiscordEmbedBuilder() 
            {
                Title = "Listing All Commands...",
                Timestamp = DateTime.UtcNow.AddHours(7),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "OSIS Discord Assistant"
                }
            };

            string description = $"{Formatter.Bold("!event")} - Commands to access the Events Manager.\n" +
                $"{Formatter.Bold("!remind")} - Reminder feature to remind yourself, a specific member or division, or everyone.\n" +
                $"{Formatter.Bold("!poll")} - Creates a poll with a set of specified emojis to choose.\n" +
                $"{Formatter.Bold("!myinfo")} - Displays the info that this bot has over you.\n" +
                $"{Formatter.Bold("!avatar")} - Shows a Discord profile picture of your account or another user.\n" +
                $"{Formatter.Bold("!slap")} - Slaps the tagged member or role.\n" +
                $"{Formatter.Bold("!setname")} - Sets a new name for yourself or another user.\n" +
                $"{Formatter.Bold("!kick")} - Kicks a member from this Discord server.\n" +
                $"{Formatter.Bold("!mute")} - Mutes a member.\n" +
                $"{Formatter.Bold("!unmute")} - Unmutes a member.\n" +
                $"{Formatter.Bold("!requestverify")} - Request a verification as a new member to the core council members.\n" +
                $"{Formatter.Bold("!overify")} - Manually verifies a new member as a council member.\n" +
                $"{Formatter.Bold("!uptime")} - Displays how long has the bot been running.\n" +
                $"{Formatter.Bold("!about")} - Shows the bot's information.\n";

            embedBuilder.WithDescription(description);
            embedBuilder.Color = DiscordColor.MidnightBlue;

            await ctx.Channel.SendMessageAsync(embed: embedBuilder).ConfigureAwait(false);
        }
    }
}
