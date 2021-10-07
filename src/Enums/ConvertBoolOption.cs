namespace OSISDiscordAssistant.Enums
{
    /// <summary>
    /// Specifies what to convert the bool value to.
    /// </summary>
    public enum ConvertBoolOption
    {
        /// <summary>
        /// Converts the bool value to 'Yes' if true or 'No' if false.
        /// </summary>
        YesOrNo,

        /// <summary>
        /// Converts the bool value to 'Expired' if true or 'Active' if false.
        /// </summary>
        ActiveOrDone,

        /// <summary>
        /// Converts the bool value to 'Stored' if true or 'Not stored' if false.
        /// </summary>
        StoredOrNotStored
    }
}
