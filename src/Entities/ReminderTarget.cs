namespace OSISDiscordAssistant.Entities
{
    public class ReminderTarget
    {
        /// <summary>
        /// The name of the member or role.
        /// </summary>
        public string Name;

        /// <summary>
        /// The display name of the member.
        /// </summary>
        public string DisplayName;

        /// <summary>
        /// The discriminator of the member.
        /// </summary>
        public string Discriminator;

        /// <summary>
        /// The mention string of the member or role.
        /// </summary>
        public string MentionString;

        /// <summary>
        /// Whether the retrieved data is a guild member or role. True if it is a guild member, otherwise false.
        /// </summary>
        public bool IsUser;
    }
}
