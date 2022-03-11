namespace OSISDiscordAssistant.Models
{
    public class Events
    {
        /// <summary>
        /// The ID of the respective row. Do not touch.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Event name to get or set.
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        /// Event person-in-charge to get or set.
        /// </summary>
        public string PersonInCharge { get; set; }

        /// <summary>
        /// The event date, which is represented as unix timestamp.
        /// </summary>
        public long EventDateUnixTimestamp { get; set; }

        /// <summary>
        /// The date when the event reminder for the respective event needs to be sent, which is represented as unix timestamp.
        /// </summary>
        public long NextScheduledReminderUnixTimestamp { get; set; }

        /// <summary>
        /// Event description to get or set.
        /// </summary>
        public string EventDescription { get; set; }

        /// <summary>
        /// The number of event reminders that has been sent. Referred to as a "level".
        /// </summary>
        public int ExecutedReminderLevel { get; set; }

        /// <summary>
        /// Marked as true if the respective event's proposal is already submitted.
        /// </summary>
        public bool ProposalReminded { get; set; }

        /// <summary>
        /// Marks an event as true if expired.
        /// </summary>
        public bool Expired { get; set; }

        /// <summary>
        /// Enable or disable sending event and proposal submission reminders.
        /// </summary>
        public bool ReminderDisabled { get; set; }

        /// <summary>
        /// Event proposal file name.
        /// </summary>
        public string ProposalFileTitle { get; set; }

        /// <summary>
        /// Event proposal file content in byte array.
        /// </summary>
        public byte[] ProposalFileContent { get; set; }
    }
}
