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
        public string TagName { get; set; }

        /// <summary>
        /// Tag content to get or set.
        /// </summary>
        public string TagContent { get; set; }
    }
}
