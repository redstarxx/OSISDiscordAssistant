namespace OSISDiscordAssistant.Models
{
    public class Verification
    {
        /// <summary>
        /// The ID of the respective row. Do not touch.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The user ID of the user requesting verification.
        /// </summary>
        public ulong UserId { get; set; }

        /// <summary>
        /// The ID of the verification request embed message.
        /// </summary>
        public ulong VerificationEmbedId { get; set; }

        /// <summary>
        /// The new nickname requested by the requesting user. Can be null.
        /// </summary>
        public string RequestedName { get; set; }
    }
}
