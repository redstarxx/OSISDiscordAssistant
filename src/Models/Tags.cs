namespace OSISDiscordAssistant.Models
{
    public class Tags
    {
        /// <summary>
        /// The ID of the respective row. Do not touch.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Tag name to get or set.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Tag content to get or set.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// The ID of the user who created this tag.
        /// </summary>
        public ulong CreatorUserId { get; set; }

        /// <summary>
        /// The ID of the last user who updated this tag, if any.
        /// </summary>
        public ulong? UpdaterUserId { get; set; }

        /// <summary>
        /// The unix timestamp of the date & time of tag creation.
        /// </summary>
        public long CreatedTimestamp { get; set; }

        /// <summary>
        /// The unix timestamp of the last time the tag was updated.
        /// </summary>
        public long? LastUpdatedTimestamp { get; set; }

        /// <summary>
        /// The number of times of how many this tag has been updated.
        /// </summary>
        public int VersionCount { get; set; }
    }
}
