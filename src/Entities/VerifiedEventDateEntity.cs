using System;
using System.Globalization;
using System.Text.RegularExpressions;
using DSharpPlus;

namespace OSISDiscordAssistant.Entities
{
    /// <summary>
    /// Provides function to verify inputted event date time string.
    /// </summary>
    public class VerifiedEventDateEntity
    {
        /// <summary>
        /// Gets whether the given inputted date time string passes the verification.
        /// </summary>
        public bool Passed { get; set; }

        /// <summary>
        /// The error message, if the verification fails.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// The <see cref="CultureInfo" /> of the given inputted date time string, if the verification is successful.
        /// </summary>
        public string DateCultureInfo { get; set; }

        /// <summary>
        /// Verifies the given date time to be either in a US or Indonesian format.
        /// </summary>
        /// <param name="eventDate"></param>
        /// <returns>The <see cref="VerifiedEventDateEntity" /> object which contains the data related to the date verification.</returns>
        public VerifiedEventDateEntity Verify(string eventDate)
        {
            Regex regex = new Regex(@"\d{4}");

            Match yearExist = regex.Match(eventDate);

            if (!yearExist.Success)
            {
                VerifiedEventDateEntity verifiedEventDateEntity = new VerifiedEventDateEntity()
                {
                    Passed = false,
                    ErrorMessage = $"{Formatter.Bold("[ERROR]")} Oops! It looks like you did not include the year of the event. Please add it! (example: 25 Juni 2021)."
                };

                return verifiedEventDateEntity;
            }

            // The following try-catch blocks will attempt to parse the given date time. 
            // If it fails, the event creation is canceled as it would not allow the bot to parse them for event reminders.
            try
            {
                var cultureInfoUS = new CultureInfo("en-US");

                VerifiedEventDateEntity verifiedEventDateEntity = new VerifiedEventDateEntity();

                DateTime desiredDateTime = DateTime.Parse(eventDate, cultureInfoUS);

                TimeSpan timeSpan = desiredDateTime - DateTime.Now;

                if (timeSpan.TotalDays > 365)
                {
                    verifiedEventDateEntity.Passed = false;
                    verifiedEventDateEntity.ErrorMessage = $"{Formatter.Bold("[ERROR]")} Maximum allowed time span is one year (365 days). Alternatively, include the year of the event as well if you have not.";

                    return verifiedEventDateEntity;
                }

                if (timeSpan.Days < 1)
                {
                    verifiedEventDateEntity.Passed = false;
                    verifiedEventDateEntity.ErrorMessage = $"{Formatter.Bold("[ERROR]")} Minimum allowed date is one day before the event. Alternatively, include the year of the event as well if you have not.";

                    return verifiedEventDateEntity;
                }

                // Set the culture info to store.
                verifiedEventDateEntity.Passed = true;
                verifiedEventDateEntity.DateCultureInfo = "en-US";

                return verifiedEventDateEntity;
            }

            catch
            {
                try
                {
                    var cultureInfoID = new CultureInfo("id-ID");

                    VerifiedEventDateEntity verifiedEventDateEntity = new VerifiedEventDateEntity();

                    DateTime desiredDateTime = DateTime.Parse(eventDate, cultureInfoID);

                    TimeSpan timeSpan = desiredDateTime - DateTime.Now;

                    if (timeSpan.TotalDays > 365)
                    {
                        verifiedEventDateEntity.Passed = false;
                        verifiedEventDateEntity.ErrorMessage = $"{Formatter.Bold("[ERROR]")} Maximum allowed time span is one year (365 days). Alternatively, include the year of the event as well if you have not.";

                        return verifiedEventDateEntity;
                    }

                    if (timeSpan.Days < 1)
                    {
                        verifiedEventDateEntity.Passed = false;
                        verifiedEventDateEntity.ErrorMessage = $"{Formatter.Bold("[ERROR]")} Minimum allowed date is one day before the event. Alternatively, include the year of the event as well if you have not.";
    
                        return verifiedEventDateEntity;
                    }

                    // Set the culture info to store.
                    verifiedEventDateEntity.Passed = true;
                    verifiedEventDateEntity.DateCultureInfo = "id-ID";

                    return verifiedEventDateEntity;
                }

                catch
                {
                    VerifiedEventDateEntity verifiedEventDateEntity = new VerifiedEventDateEntity()
                    {
                        Passed = false,
                        ErrorMessage = $"{Formatter.Bold("[ERROR]")} An error occured. Your event date cannot be parsed. Make sure your date and time is written in English or Indonesian format. Example: {Formatter.InlineCode("25 June 2021.")} or {Formatter.InlineCode("25 Juni 2021.")}"
                    };

                    return verifiedEventDateEntity;
                }
            }
        }
    }
}
