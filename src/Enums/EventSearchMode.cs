namespace OSISDiscordAssistant.Enums
{
    /// <summary>
    /// Specifies the strategy used to search for the event data.
    /// </summary>
    public enum EventSearchMode
    {
        /// <summary>
        /// Search for an event with the exact name.
        /// </summary>
        Exact,

        /// <summary>
        /// Search for an event with the closest matching name.
        /// </summary>
        ClosestMatching
    }
}
