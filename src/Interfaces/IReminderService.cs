using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using OSISDiscordAssistant.Models;

namespace OSISDiscordAssistant.Services
{
    public interface IReminderService
    {
        Task<string> CreateReminderMessage(Reminder reminder);
        Task RegisterReminder(TimeSpan remainingTime, DiscordChannel targetChannel, string remindMessage, string remindTarget, string displayTarget, CommandContext commandContext = null, InteractionContext interactionContext = null);
        Task SendGuildReminders(CommandContext commandContext = null, InteractionContext interactionContext = null);
        Task SendTimespanHelpEmbedAsync(CommandContext commandContext = null, InteractionContext interactionContext = null);
        void Start();
    }
}