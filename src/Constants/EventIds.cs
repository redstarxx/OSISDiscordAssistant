using Microsoft.Extensions.Logging;

namespace OSISDiscordAssistant.Constants
{
    public static class EventIds
    {
        // TODO: REPLACE ALL OLD EVENTIDS WITH THESE ONES.
        public static EventId
            Core = new(0, "Core"),
            Services = new(1, "Services"),
            EventHandler = new(2, "EventHandler"),
            CommandHandler = new(3, "CommandHandler"),
            StatusUpdater = new(4, "StatusUpdater");
    }
}
