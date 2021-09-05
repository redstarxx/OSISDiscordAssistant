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
        /// Event date to get or set.
        /// </summary>
        public string EventDate { get; set; }

        /// <summary>
        /// Event date culture info to get or set.
        /// </summary>
        public string EventDateCultureInfo { get; set; }

        /// <summary>
        /// Event description to get or set.
        /// </summary>
        public string EventDescription { get; set; }

        /// <summary>
        /// Marked as true if the respective event's proposal is already submitted.
        /// </summary>
        public bool ProposalReminded { get; set; }

        /// <summary>
        /// Marks an event as true if already reminded before D-Day.
        /// </summary>
        public bool PreviouslyReminded { get; set; }

        /// <summary>
        /// Marks an event as true if expired.
        /// </summary>
        public bool Expired { get; set; }

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
