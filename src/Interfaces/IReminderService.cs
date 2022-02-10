using OSISDiscordAssistant.Models;
using System;
using System.Threading.Tasks;

namespace OSISDiscordAssistant.Services
{
    public interface IReminderService
    {
        Task<string> CreateReminderMessage(Reminder reminder);
        void CreateReminderTask(Reminder reminder, TimeSpan remainingTime);
        void Start();
    }
}