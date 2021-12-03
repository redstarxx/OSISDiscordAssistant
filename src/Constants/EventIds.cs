using Microsoft.Extensions.Logging;

namespace OSISDiscordAssistant.Constants
{
    public static class EventIds
    {
        // TODO: REPLACE ALL OLD EVENTIDS WITH THESE ONES.
        public static EventId
            Core = new(0, "Core"),
            Services = new(1, "Services"),
            Database = new(2, "Database"),
            EventHandler = new(3, "EventHandler"),
            StatusUpdater = new(4, "StatusUpdater"),
            CommandHandler = new(5, "CommandHandler");
    }
}
