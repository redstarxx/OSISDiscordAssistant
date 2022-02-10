namespace OSISDiscordAssistant.Models
{
    public class Reminder
    {
        /// <summary>
        /// The ID of the respective row. Do not touch.
        /// </summary>
        public int Id { get; set; }

        public ulong InitiatingUserId { get; set; }

        public string TargetedUserOrRoleMention { get; set; }

        public long UnixTimestampRemindAt { get; set; }

        public ulong TargetGuildId { get; set; }

        public ulong? ReplyMessageId { get; set; }

        public ulong? TargetChannelId { get; set; }

        public bool? Cancelled { get; set; }

        public string Content { get; set; }
    }
}
