using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Entities;
using DSharpPlus;
using System.Globalization;
using Npgsql;
using System.Text.RegularExpressions;
using System.Threading;
using Humanizer;
using discordbot;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace discordbot.Commands
{
    class EventCommandsModule : BaseCommandModule
    {
        /// <summary>
        /// Commands to operate the Events Manager's create or list option.
        /// </summary>
        /// <param name="ctx">The respective context that the command belongs to.</param>
        /// <param name="operationSelection">Operation type to run.</param>
        [Command("event")]
        public async Task EventCreateOrList(CommandContext ctx, string operationSelection)
        {
            if (operationSelection == "create")
            {
                string eventName = null;
                string personInCharge = null;
                string eventDate = null;
                string eventDateCultureInfo = null;
                string eventDescription = null;

                await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, enter your event name. You have one minute.").ConfigureAwait(false);
                var interactivityModule = ctx.Client.GetInteractivity();
                var eventNameResult = await interactivityModule.WaitForMessageAsync
                    (x => x.Author.Id == ctx.User.Id && (x.Channel.Id == ctx.Channel.Id), TimeSpan.FromMinutes(1));

                if (!eventNameResult.TimedOut)
                {
                    eventName = eventNameResult.Result.Content;
                    if (eventName.Length > 50)
                    {
                        await ctx.Channel.SendMessageAsync("**[ERROR]** Maximum character limit of 50 characters exceeded. You must re-run the command to finish.").ConfigureAwait(false);
                        return;
                    }

                    await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, tag the person-in-charge (ketua / wakil ketua acara) for {Formatter.Bold(eventNameResult.Result.Content)}. You have one minute.").ConfigureAwait(false);
                    var personInChargeResult = await interactivityModule.WaitForMessageAsync
                        (x => x.Author.Id == ctx.User.Id && x.Channel.Id == ctx.Channel.Id, TimeSpan.FromMinutes(1));

                    if (!personInChargeResult.TimedOut)
                    {
                        personInCharge = personInChargeResult.Result.Content;
                        if (eventName.Length > 100)
                        {
                            await ctx.Channel.SendMessageAsync("**[ERROR]** Maximum character limit of 100 characters exceeded. You must re-run the command to finish.").ConfigureAwait(false);
                            return;
                        }

                        await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, enter your event date for {Formatter.Bold(eventNameResult.Result.Content)}. You have one minute.").ConfigureAwait(false);
                        var eventDateResult = await interactivityModule.WaitForMessageAsync
                            (x => x.Author.Id == ctx.User.Id && x.Channel.Id == ctx.Channel.Id, TimeSpan.FromMinutes(1));

                        if (!eventDateResult.TimedOut)
                        {
                            eventDate = eventDateResult.Result.Content;
                            if (eventDate.Length > 50)
                            {
                                await ctx.Channel.SendMessageAsync("**[ERROR]** Maximum character limit of 50 characters exceeded. You must re-run the command to finish.").ConfigureAwait(false);
                                return;
                            }

                            // The following try-catch blocks will attempt to parse the given date time. 
                            // If it fails, the event creation is canceled as it would not allow the bot to parse them for event reminders.
                            try
                            {
                                var cultureInfoID = new CultureInfo("id-ID");
                                DateTime toConvert = DateTime.Parse(eventDate, cultureInfoID);

                                TimeSpan calculateTimeSpan = toConvert - DateTime.Now;

                                if (calculateTimeSpan.Days > 365)
                                {
                                    string errorMessage = "**[ERROR]** Maximum allowed time span is one year (365 days).";
                                    await ctx.Channel.SendMessageAsync(errorMessage).ConfigureAwait(false);

                                    return;
                                }

                                if (calculateTimeSpan.Days < 1)
                                {
                                    string errorMessage = "**[ERROR]** Minimum allowed date is one day before the event. Alternatively, include the year of the event as well if you have not.";
                                    await ctx.Channel.SendMessageAsync(errorMessage).ConfigureAwait(false);

                                    return;
                                }

                                // Set the culture info to store.
                                eventDateCultureInfo = "id-ID";
                            }

                            catch
                            {
                                try
                                {
                                    var cultureInfoUS = new CultureInfo("en-US");
                                    DateTime toConvert = DateTime.Parse(eventDate, cultureInfoUS);

                                    TimeSpan calculateTimeSpan = toConvert - DateTime.Now;

                                    if (calculateTimeSpan.TotalDays > 365)
                                    {
                                        string errorMessage = "**[ERROR]** Maximum allowed time span is one year (365 days).";
                                        await ctx.RespondAsync(errorMessage).ConfigureAwait(false);

                                        return;
                                    }

                                    if (calculateTimeSpan.Days < 1)
                                    {
                                        string errorMessage = "**[ERROR]** Minimum allowed date is one day before the event. Alternatively, include the year of the event as well if you have not.";
                                        await ctx.Channel.SendMessageAsync(errorMessage).ConfigureAwait(false);

                                        return;
                                    }

                                    // Set the culture info to store.
                                    eventDateCultureInfo = "en-US";
                                }

                                catch
                                {
                                    // Notify the user that the provided event date cannot be parsed.
                                    await ctx.Channel.SendMessageAsync("**[ERROR]** An error occured. Your event date cannot be parsed. Make sure your date and time is written in either Indonesian or English format.").ConfigureAwait(false);
                                    return;
                                }
                            }

                            await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, enter your event description for {Formatter.Bold(eventNameResult.Result.Content)}. You have one minute.").ConfigureAwait(false);
                            var eventDescriptionResult = await interactivityModule.WaitForMessageAsync
                                (x => x.Author.Id == ctx.User.Id && x.Channel.Id == ctx.Channel.Id, TimeSpan.FromMinutes(1));

                            if (!eventDescriptionResult.TimedOut)
                            {
                                eventDescription = eventDescriptionResult.Result.Content;
                                if (eventDescription.Length > 255)
                                {
                                    await ctx.Channel.SendMessageAsync("**[ERROR]** Maximum character limit of 255 characters exceeded. You must re-run the command to finish.").ConfigureAwait(false);
                                    return;
                                }
                            }

                            else
                            {
                                await ctx.Channel.SendMessageAsync("**[TIMED OUT]** Event description not entered within given time span. Re-run the command if you still need to create your event.").ConfigureAwait(false);
                                return;
                            }
                        }
                    }

                    else
                    {
                        await ctx.Channel.SendMessageAsync("**[TIMED OUT]** Person-in-charge not entered within given time span. Re-run the command if you still need to create your event.").ConfigureAwait(false);
                        return;
                    }
                }

                else
                {
                    await ctx.Channel.SendMessageAsync("**[TIMED OUT]** Event name not entered within given time span. Re-run the command if you still need to create your event.").ConfigureAwait(false);
                    return;
                }

                Task offloadToTask = Task.Run(async () =>
                {
                    using (var db = new EventContext())
                    {
                        db.Add(new Events
                        {
                            EventName = eventName,
                            PersonInCharge = personInCharge,
                            EventDate = eventDate,
                            EventDateCultureInfo = eventDateCultureInfo,
                            EventDescription = eventDescription
                        });

                        db.SaveChanges();

                        await db.DisposeAsync();
                    }

                    await ctx.Channel.SendMessageAsync($"Okay {ctx.Member.Mention}, your event, {Formatter.Bold(eventName)} has been created.").ConfigureAwait(false);
                });
            }

            else if (operationSelection == "list")
            {
                var embedBuilder = new DiscordEmbedBuilder
                {
                    Title = "Events Manager - Listing All Events...",
                    Timestamp = DateTime.Now.AddHours(7),
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = "OSIS Discord Assistant"
                    }
                };

                var notifyMessage = await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[EVENTS MANAGER]")} Give me a second to process everything...").ConfigureAwait(false);
                await ctx.TriggerTypingAsync();

                Task offloadToTask = Task.Run(async () =>
                {
                    int eventIndex = 0;
                    using (var db = new EventContext())
                    {
                        foreach (var events in db.Events)
                        {
                            string descriptionField = $"Status: {ClientUtilities.ConvertStatusBoolean(events.Expired)}\nPerson-in-charge: {events.PersonInCharge}\nDescription: {events.EventDescription}";
                            embedBuilder.AddField($"({events.Id}) {events.EventName} [{events.EventDate}]", descriptionField, true);
                            eventIndex++;
                        }
                    }

                    if (eventIndex == 0)
                    {
                        embedBuilder.Description = "There are no events to list.";
                    }

                    else
                    {
                        embedBuilder.Description = $"List of all registered events. In total, there are {eventIndex} ({eventIndex.ToWords()}) events.";
                    }

                    await notifyMessage.DeleteAsync();
                    await ctx.Channel.SendMessageAsync(embed: embedBuilder).ConfigureAwait(false);
                });
            }

            else
            {
                return;
            }
        }

        /// <summary>
        /// Commands to operate the Events Manager's update or delete or search option.
        /// </summary>
        /// <param name="ctx">The respective context that the command belongs to.</param>
        /// <param name="operationSelection">Operation type to run.</param>
        /// <param name="optionalInput">Row number or event name from the events table to update or delete or search. Optional.</param>
        [Command("event")]
        public async Task Event(CommandContext ctx, string operationSelection, params string[] optionalInput)
        {                    
            if (operationSelection == "update")
            {
                var embedBuilder = new DiscordEmbedBuilder
                {
                    Timestamp = DateTime.Now,
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = "OSIS Discord Assistant"
                    }
                };

                try
                {                   
                    using (var db = new EventContext())
                    {
                        bool isNumber = int.TryParse(string.Join(" ", optionalInput), out int rowIDRaw);
                        bool rowExists = false;

                        int rowID = 0;
                        string inputEventName = string.Join(" ", optionalInput);

                        if (isNumber)
                        {
                            rowExists = db.Events.Any(x => x.Id == rowIDRaw);
                            rowID = db.Events.SingleOrDefault(x => x.Id == rowIDRaw).Id;
                        }

                        else if (!isNumber)
                        {
                            int counter = 0;

                            foreach (var events in db.Events)
                            {
                                if (events.EventName.ToLowerInvariant() == inputEventName.ToLowerInvariant())
                                {
                                    rowID = events.Id;
                                    rowExists = true;
                                    counter++;

                                    break;
                                }
                            }

                            if (counter == 0)
                            {
                                string errorMessage = $"{Formatter.Bold("[ERROR]")} An error occured. You must provide the correct ID or name of the event you are updating. Refer to {Formatter.InlineCode("!event list")} or {Formatter.InlineCode("!event search")}.";
                                await ctx.Channel.SendMessageAsync(errorMessage).ConfigureAwait(false);

                                return;
                            }
                        }

                        // Checks whether the selected event is already expired
                        bool hasExpired = db.Events.SingleOrDefault(x => x.Id == rowID).Expired;

                        if (hasExpired)
                        {
                            string errorMessage = $"{Formatter.Bold("[ERROR]")} Oops! You can only update events that has not yet expired.";
                            await ctx.Channel.SendMessageAsync(errorMessage).ConfigureAwait(false);

                            return;
                        }

                        if (rowExists)
                        {
                            string previousEventName = db.Events.SingleOrDefault(x => x.Id == rowID).EventName;

                            embedBuilder.Title = $"Events Manager - Updating {previousEventName}...";
                            embedBuilder.Description = $"Choose either one of the following emojis to select what are you going to change from {Formatter.Bold(previousEventName)}.\n\n" +
                                "**[1]** Change event name;\n**[2]** Change event person-in-charge (ketua / wakil ketua acara);\n**[3]** Change event date and time;\n**[4]** Change event description.\n\n" +
                                $"You have 5 (five) minutes to make your choice otherwise the bot will abort. To cancel your changes, type {Formatter.InlineCode("abort")}.";
                            var updateEmbed = await ctx.Channel.SendMessageAsync(embed: embedBuilder).ConfigureAwait(false);

                            var numberOneEmoji = DiscordEmoji.FromName(ctx.Client, ":one:");
                            var numberTwoEmoji = DiscordEmoji.FromName(ctx.Client, ":two:");
                            var numberThreeEmoji = DiscordEmoji.FromName(ctx.Client, ":three:");
                            var numberFourEmoji = DiscordEmoji.FromName(ctx.Client, ":four:");

                            await updateEmbed.CreateReactionAsync(numberOneEmoji).ConfigureAwait(false);
                            await updateEmbed.CreateReactionAsync(numberTwoEmoji).ConfigureAwait(false);
                            await updateEmbed.CreateReactionAsync(numberThreeEmoji).ConfigureAwait(false);
                            await updateEmbed.CreateReactionAsync(numberFourEmoji).ConfigureAwait(false);

                            var selectionInteractivity = ctx.Client.GetInteractivity();
                            var selectionResult = await selectionInteractivity.WaitForReactionAsync
                                (x => x.Message == updateEmbed && (x.User.Id == ctx.User.Id) && (x.Emoji == numberOneEmoji || x.Emoji == numberTwoEmoji ||
                                x.Emoji == numberThreeEmoji || x.Emoji == numberFourEmoji), TimeSpan.FromMinutes(5));

                            if (!selectionResult.TimedOut)
                            {
                                string eventName = null;
                                string personInCharge = null;
                                string eventDate = null;
                                string eventDateCultureInfo = null;
                                string eventDescription = null;

                                if (selectionResult.Result.Emoji == numberOneEmoji)
                                {
                                    await updateEmbed.DeleteAllReactionsAsync();

                                    var inputInteractivity = ctx.Client.GetInteractivity();

                                    var updateNameMessage = await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, enter your new event name for {Formatter.Bold(previousEventName)}. You have one minute.").ConfigureAwait(false);
                                    var eventNameResult = await inputInteractivity.WaitForMessageAsync
                                        (x => x.Author.Id == ctx.User.Id && x.Channel.Id == ctx.Channel.Id, TimeSpan.FromMinutes(1));

                                    if (!eventNameResult.TimedOut)
                                    {
                                        eventName = eventNameResult.Result.Content;
                                        if (eventName.Length > 50)
                                        {
                                            await ctx.Channel.SendMessageAsync("**[ERROR]** Operation aborted. Maximum character limit of 50 characters exceeded.").ConfigureAwait(false);
                                            return;
                                        }

                                        if (eventName == "abort")
                                        {
                                            await ctx.Channel.SendMessageAsync($"Update operation aborted as per your request, {ctx.Member.Mention}.").ConfigureAwait(false);
                                            return;
                                        }

                                        Task offloadToTask = Task.Run(async () =>
                                        {
                                            using (var dbUpdate = new EventContext())
                                            {
                                                Events rowToUpdate = null;
                                                rowToUpdate = dbUpdate.Events.SingleOrDefault(x => x.Id == rowID);

                                                if (rowToUpdate != null)
                                                {
                                                    rowToUpdate.EventName = eventName;
                                                }

                                                dbUpdate.SaveChanges();

                                                await dbUpdate.DisposeAsync();
                                            }

                                            embedBuilder.Title = $"Events Manager - {previousEventName} Update Details";
                                            embedBuilder.Description = $"{ctx.Member.Mention} has made update(s) to {previousEventName}.\n\n{Formatter.Bold("Changes made:")}\n• Changed event name from {Formatter.InlineCode(previousEventName)} to {Formatter.InlineCode(eventName)}.";
                                            embedBuilder.Timestamp = DateTime.Now;

                                            await ctx.Channel.SendMessageAsync(embed: embedBuilder).ConfigureAwait(false);
                                        });
                                    }

                                    else
                                    {
                                        await ctx.Channel.SendMessageAsync("**[TIMED OUT]** Operation aborted. Event name not entered within given time span.").ConfigureAwait(false);
                                        return;
                                    }
                                }

                                else if (selectionResult.Result.Emoji == numberTwoEmoji)
                                {
                                    await updateEmbed.DeleteAllReactionsAsync();

                                    string previousPersonInCharge = db.Events.SingleOrDefault(x => x.Id == rowID).PersonInCharge;

                                    var inputInteractivity = ctx.Client.GetInteractivity();

                                    var updatePersonInChargeMessage = await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, enter your new person-in-charge for {Formatter.Bold(previousEventName)}. You have one minute.").ConfigureAwait(false);
                                    var eventPersonInChargeResult = await inputInteractivity.WaitForMessageAsync
                                        (x => x.Author.Id == ctx.User.Id && x.Channel.Id == ctx.Channel.Id, TimeSpan.FromMinutes(1));

                                    if (!eventPersonInChargeResult.TimedOut)
                                    {
                                        personInCharge = eventPersonInChargeResult.Result.Content;
                                        if (eventName.Length > 100)
                                        {
                                            await ctx.Channel.SendMessageAsync("**[ERROR]** Operation aborted. Maximum character limit of 100 characters exceeded.").ConfigureAwait(false);
                                            return;
                                        }

                                        if (eventName == "abort")
                                        {
                                            await ctx.Channel.SendMessageAsync($"Update operation aborted as per your request, {ctx.Member.Mention}.").ConfigureAwait(false);
                                            return;
                                        }

                                        Task offloadToTask = Task.Run(async () =>
                                        {
                                            using (var dbUpdate = new EventContext())
                                            {
                                                Events rowToUpdate = null;
                                                rowToUpdate = dbUpdate.Events.SingleOrDefault(x => x.Id == rowID);

                                                if (rowToUpdate != null)
                                                {
                                                    rowToUpdate.PersonInCharge = personInCharge;
                                                }

                                                dbUpdate.SaveChanges();

                                                await dbUpdate.DisposeAsync();
                                            }

                                            embedBuilder.Title = $"Events Manager - {previousEventName} Update Details";
                                            embedBuilder.Description = $"{ctx.Member.Mention} has made update(s) to {previousEventName}.\n\n{Formatter.Bold("Changes made:")}\n• Changed person-in-charge (ketua / wakil ketua event) from {Formatter.InlineCode(previousPersonInCharge)} to {Formatter.InlineCode(personInCharge)}.";
                                            embedBuilder.Timestamp = DateTime.Now;

                                            await ctx.Channel.SendMessageAsync(embed: embedBuilder).ConfigureAwait(false);
                                        });                                       
                                    }

                                    else
                                    {
                                        await ctx.Channel.SendMessageAsync("**[TIMED OUT]** Operation aborted. Event name not entered within given time span.").ConfigureAwait(false);
                                        return;
                                    }
                                }

                                else if (selectionResult.Result.Emoji == numberThreeEmoji)
                                {
                                    await updateEmbed.DeleteAllReactionsAsync();

                                    string previousEventDate = db.Events.SingleOrDefault(x => x.Id == rowID).EventDate;

                                    var inputInteractivity = ctx.Client.GetInteractivity();

                                    var updateDateMessage = await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, enter your new event date for {Formatter.Bold(previousEventName)}. You have one minute.").ConfigureAwait(false);
                                    var eventDateResult = await inputInteractivity.WaitForMessageAsync
                                        (x => x.Author.Id == ctx.User.Id && x.Channel.Id == ctx.Channel.Id, TimeSpan.FromMinutes(1));

                                    if (!eventDateResult.TimedOut)
                                    {
                                        eventDate = eventDateResult.Result.Content;
                                        if (eventDate.Length > 50)
                                        {
                                            await ctx.Channel.SendMessageAsync("**[ERROR]** Operation aborted. Maximum character limit of 50 characters exceeded.").ConfigureAwait(false);
                                            return;
                                        }

                                        if (eventDate == "abort")
                                        {
                                            await ctx.Channel.SendMessageAsync($"Update operation aborted as per your request, {ctx.Member.Mention}.").ConfigureAwait(false);
                                            return;
                                        }

                                        // The following try-catch blocks will attempt to parse the given date time.
                                        // If it fails, the event creation is canceled as it would not allow the bot to parse them for event reminders.
                                        try
                                        {
                                            var cultureInfoID = new CultureInfo("id-ID");
                                            DateTime toConvert = DateTime.Parse(eventDate, cultureInfoID);

                                            TimeSpan calculateTimeSpan = toConvert - DateTime.Now;

                                            if (calculateTimeSpan.TotalDays > 365)
                                            {
                                                string errorMessage = "**[ERROR]** Maximum allowed time span is one year (365 days).";
                                                await ctx.RespondAsync(errorMessage).ConfigureAwait(false);
                                                return;
                                            }

                                            else
                                            {
                                                // Set the culture info to store.
                                                eventDateCultureInfo = "id-ID";
                                            }
                                        }

                                        catch
                                        {
                                            try
                                            {
                                                var cultureInfoUS = new CultureInfo("en-US");
                                                DateTime toConvert = DateTime.Parse(eventDate, cultureInfoUS);

                                                TimeSpan calculateTimeSpan = toConvert - DateTime.Now;

                                                if (calculateTimeSpan.TotalDays > 365)
                                                {
                                                    string errorMessage = "**[ERROR]** Maximum allowed time span is one year (365 days).";
                                                    await ctx.RespondAsync(errorMessage).ConfigureAwait(false);
                                                    return;
                                                }

                                                else
                                                {
                                                    // Set the culture info to store.
                                                    eventDateCultureInfo = "en-US";
                                                }
                                            }

                                            catch
                                            {
                                                // Notify the user that the provided event date cannot be parsed.
                                                await ctx.Channel.SendMessageAsync("**[ERROR]** An error occured. Your event date cannot be parsed. Make sure your date and time is written in either English or Indonesian format. Example: 15 November 2021 07:00, 25 Juni 2022.").ConfigureAwait(false);
                                                return;
                                            }
                                        }

                                        Task offloadToTask = Task.Run(async () =>
                                        {
                                            using (var dbUpdate = new EventContext())
                                            {
                                                Events rowToUpdate = null;
                                                rowToUpdate = dbUpdate.Events.SingleOrDefault(x => x.Id == rowID);

                                                if (rowToUpdate != null)
                                                {
                                                    rowToUpdate.EventDate = eventDate;
                                                    rowToUpdate.EventDateCultureInfo = eventDateCultureInfo;
                                                    rowToUpdate.PreviouslyReminded = false;

                                                    dbUpdate.SaveChanges();

                                                    await dbUpdate.DisposeAsync();
                                                }

                                                embedBuilder.Title = $"Events Manager - {previousEventName} Update Details";
                                                embedBuilder.Description = $"{ctx.Member.Mention} has made update(s) to {previousEventName}.\n\n{Formatter.Bold("Changes made:")}\n• Changed event date from {Formatter.InlineCode(previousEventDate)} to {Formatter.InlineCode(eventDate)}.";
                                                embedBuilder.Timestamp = DateTime.Now;

                                                await ctx.Channel.SendMessageAsync(embed: embedBuilder).ConfigureAwait(false);
                                            }
                                        });                                       
                                    }

                                    else
                                    {
                                        await ctx.Channel.SendMessageAsync("**[TIMED OUT]** Operation aborted. Event date not entered within given time span.").ConfigureAwait(false);
                                        return;
                                    }
                                }

                                else if (selectionResult.Result.Emoji == numberFourEmoji)
                                {
                                    await updateEmbed.DeleteAllReactionsAsync();

                                    string previousEventDescription = db.Events.SingleOrDefault(x => x.Id == rowID).EventDescription;

                                    var inputInteractivity = ctx.Client.GetInteractivity();

                                    var updateDescriptionMessage = await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, enter your new event description for {Formatter.Bold(previousEventName)}. You have one minute.").ConfigureAwait(false);
                                    var eventDescriptionResult = await inputInteractivity.WaitForMessageAsync
                                        (x => x.Author.Id == ctx.User.Id && x.Channel.Id == ctx.Channel.Id, TimeSpan.FromMinutes(1));

                                    if (!eventDescriptionResult.TimedOut)
                                    {
                                        eventDescription = eventDescriptionResult.Result.Content;
                                        if (eventDescription.Length > 254)
                                        {
                                            await ctx.Channel.SendMessageAsync("**[ERROR]** Operation aborted. Maximum character limit of 255 characters exceeded.").ConfigureAwait(false);
                                            return;
                                        }

                                        if (eventDescription == "abort")
                                        {
                                            await ctx.Channel.SendMessageAsync($"Update operation aborted as per your request, {ctx.Member.Mention}.").ConfigureAwait(false);
                                            return;
                                        }

                                        Task offloadToTask = Task.Run(async () =>
                                        {
                                            using (var dbUpdate = new EventContext())
                                            {
                                                Events rowToUpdate = null;
                                                rowToUpdate = dbUpdate.Events.SingleOrDefault(x => x.Id == rowID);

                                                if (rowToUpdate != null)
                                                {
                                                    rowToUpdate.EventDescription = eventDescription;

                                                    dbUpdate.SaveChanges();

                                                    await dbUpdate.DisposeAsync();
                                                }

                                                embedBuilder.Title = $"Events Manager - {previousEventName} Update Details";
                                                embedBuilder.Description = $"{ctx.Member.Mention} has made update(s) to {previousEventName}.\n\n{Formatter.Bold("Changes made:")}\n• Changed event description from {Formatter.InlineCode(previousEventDescription)} to {Formatter.InlineCode(eventDescription)}.";
                                                embedBuilder.Timestamp = DateTime.Now;

                                                await ctx.Channel.SendMessageAsync(embed: embedBuilder).ConfigureAwait(false);
                                            }
                                        });
                                    }

                                    else
                                    {
                                        await ctx.Channel.SendMessageAsync("**[TIMED OUT]** Operation aborted. Event date not entered within given time span.").ConfigureAwait(false);
                                        return;
                                    }
                                }
                            }

                            else
                            {
                                await ctx.Channel.SendMessageAsync($"**[TIMED OUT]** Update selection for {previousEventName} not selected within given time span. Re-run the command if you still need to update your event.").ConfigureAwait(false);
                            }
                        }

                        else
                        {
                            embedBuilder.Title = "Events Manager - Update Error";
                            embedBuilder.Description = $"Event ID {Formatter.Bold(rowID.ToString())} does not exist. Make sure you are selecting an event ID from Events Manager - List.";
                            await ctx.Channel.SendMessageAsync(embed: embedBuilder).ConfigureAwait(false);
                        }

                    }
                }

                catch (PostgresException ex)
                {
                    embedBuilder.Title = "Events Manager - Update Error";
                    embedBuilder.Description = $"An error occured while working with the database. Exception details: {ex.Message}.";
                    await ctx.RespondAsync(embed: embedBuilder).ConfigureAwait(false);
                }
            }

            else if (operationSelection == "delete")
            {
                int? rowID = Convert.ToInt32(optionalInput);

                // Stops executing the remainder of the code if rowID is null.
                if (rowID == null)
                {
                    return;
                }

                try
                {
                    using (var db = new EventContext())
                    {
                        var rowToDelete = db.Events.SingleOrDefault(x => x.Id == rowID);
                        var eventNameQuery = db.Events.SingleOrDefault(x => x.Id == rowID).EventName;

                        if (rowToDelete != null)
                        {
                            db.Remove(rowToDelete);

                            db.SaveChanges();

                            await ctx.Channel.SendMessageAsync($"{Formatter.Bold(eventNameQuery.ToString())} (Event ID: {rowID.ToString()}) successfully deleted from Events Manager.").ConfigureAwait(false);
                        }

                        else
                        {
                            await ctx.Channel.SendMessageAsync($"Event ID {Formatter.Bold(rowID.ToString())} does not exist. Make sure you are selecting an event ID from Events Manager - List.").ConfigureAwait(false);
                        }
                    }
                }

                catch (Exception ex)
                {
                    await ctx.Channel.SendMessageAsync($"An error occured while working with the database.\nException details: {ex.Message}.").ConfigureAwait(false);
                }
            }

            else if (operationSelection == "search")
            {
                string parseOptionalInput = string.Join(" ", optionalInput);

                // Converts the optionalInput into a number so we can access our targeted row.
                // If true, execute query based on row ID. If false, execute query based on provided event name.
                bool isNumber = int.TryParse(parseOptionalInput, out int rowID);

                bool isLetter = parseOptionalInput.All(char.IsLetter);

                var embedBuilder = new DiscordEmbedBuilder
                {
                    Title = "Events Manager - Search Result",
                    Timestamp = DateTime.Now.AddHours(7),
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = "OSIS Discord Assistant"
                    }
                };

                try
                {
                    if (isNumber)
                    {
                        using (var db = new EventContext())
                        {
                            Events rowToRead = null;

                            try
                            {
                                rowToRead = db.Events.SingleOrDefault(x => x.Id == rowID);

                                embedBuilder.Description = $"Showing search result for event ID {Formatter.InlineCode(rowID.ToString())}...";

                                string descriptionField = $"Status: {ClientUtilities.ConvertStatusBoolean(rowToRead.Expired)}\nPerson-in-charge: {rowToRead.PersonInCharge}\nDescription: {rowToRead.EventDescription}";
                                embedBuilder.AddField($"({rowToRead.Id}) {rowToRead.EventName} [{rowToRead.EventDate}]", descriptionField, true);

                                await ctx.Channel.SendMessageAsync(embed: embedBuilder).ConfigureAwait(false);
                            }

                            catch
                            {
                                embedBuilder.Description = $"Oops! There are no results for event ID {Formatter.InlineCode(rowID.ToString())}! Have you specified the correct event ID?";
                                await ctx.Channel.SendMessageAsync(embed: embedBuilder).ConfigureAwait(false);                               

                                return;
                            }
                        }
                    }

                    else if (!isNumber)
                    {
                        string toSearch = parseOptionalInput.ToLowerInvariant();

                        using (var db = new EventContext())
                        {
                            try
                            {
                                int counter = 0;
                                foreach (var events in db.Events)
                                {
                                    if (events.EventName.ToLowerInvariant().Contains(toSearch))
                                    {
                                        string descriptionField = $"Status: {ClientUtilities.ConvertStatusBoolean(events.Expired)}\nPerson-in-charge: {events.PersonInCharge}\nDescription: {events.EventDescription}";
                                        embedBuilder.AddField($"({events.Id}) {events.EventName} [{events.EventDate}]", descriptionField, true);

                                        counter++;
                                    }
                                }

                                if (counter == 0)
                                {
                                    embedBuilder.Description = $"Oops! There are no results for event name {Formatter.InlineCode(parseOptionalInput)}! Have you typed the event name correctly?";
                                }

                                else
                                {
                                    embedBuilder.Description = $"Showing {counter} ({counter.ToWords()}) query result for event name {Formatter.InlineCode(parseOptionalInput)}...";
                                }

                                await ctx.Channel.SendMessageAsync(embed: embedBuilder).ConfigureAwait(false);
                            }

                            catch
                            {
                                embedBuilder.Description = $"Oops! There are no results for event name {Formatter.InlineCode(parseOptionalInput)}! Did you typed the correct event name?";
                                await ctx.Channel.SendMessageAsync(embed: embedBuilder).ConfigureAwait(false);

                                return;
                            }                           
                        }
                    }
                }

                catch (PostgresException ex)
                {
                    embedBuilder.ClearFields();

                    embedBuilder.Title = "Events Manager - Error";
                    embedBuilder.Description = $"An error occured while working with the database.\nException details: {ex.Message}.";

                    await ctx.Channel.SendMessageAsync(embed: embedBuilder).ConfigureAwait(false);
                }

                catch (Exception ex)
                {
                    embedBuilder.ClearFields();

                    embedBuilder.Title = "Events Manager - Error";
                    embedBuilder.Description = $"An error occured while working with the database.\nException details: {ex.Message}.";

                    await ctx.Channel.SendMessageAsync(embed: embedBuilder).ConfigureAwait(false);
                }                             
            }
           
            else
            {
                return;
            }
        }

        [Command("rawdata")]
        public async Task ReadDb(CommandContext ctx)
        {
            var connectionString = await ClientUtilities.GetConnectionStringAsync();

            using var dbConnection = new NpgsqlConnection(connectionString);
            await dbConnection.OpenAsync();

            using var dbQuery = new NpgsqlCommand("SELECT * FROM events", dbConnection);

            using NpgsqlDataReader dbReader = await dbQuery.ExecuteReaderAsync();

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append($"{dbReader.GetName(0)} {dbReader.GetName(1)} {dbReader.GetName(2)} {dbReader.GetName(3)} {dbReader.GetName(4)} {dbReader.GetName(5)} {dbReader.GetName(6)} {dbReader.GetName(7)} {dbReader.GetName(8)}\n");
            while (await dbReader.ReadAsync())
            {
                stringBuilder.Append($"{dbReader.GetInt32(0)} {dbReader.GetString(1)} {dbReader.GetString(2)} {dbReader.GetString(3)} {dbReader.GetString(4)} {dbReader.GetString(5)} {dbReader.GetBoolean(6)} {dbReader.GetBoolean(7)} {dbReader.GetBoolean(8)}\n");
            }

            await dbReader.DisposeAsync();

            dbConnection.Close();

            await ctx.RespondAsync(stringBuilder.ToString()).ConfigureAwait(false);
        }

        [Command("createtable")]
        public async Task CreateTable(CommandContext ctx)
        {
            var connectionString = await ClientUtilities.GetConnectionStringAsync();

            using var dbConnection = new NpgsqlConnection(connectionString);
            dbConnection.Open();

            var dbQuery = new NpgsqlCommand("CREATE TABLE IF NOT EXISTS events(id SERIAL PRIMARY KEY, event_name VARCHAR(50), person_in_charge VARCHAR(100), event_date VARCHAR(50), event_date_culture_info VARCHAR(10), event_description VARCHAR(255), proposal_reminded BOOLEAN NOT NULL, previously_reminded BOOLEAN NOT NULL, expired BOOLEAN NOT NULL)", dbConnection);
            dbQuery.ExecuteNonQuery();

            dbQuery = new NpgsqlCommand("ALTER TABLE events ALTER COLUMN previously_reminded SET DEFAULT FALSE", dbConnection);
            dbQuery.ExecuteNonQuery();

            dbQuery = new NpgsqlCommand("ALTER TABLE events ALTER COLUMN expired SET DEFAULT FALSE", dbConnection);
            dbQuery.ExecuteNonQuery();

            dbQuery = new NpgsqlCommand("ALTER TABLE events ALTER COLUMN proposal_reminded SET DEFAULT FALSE", dbConnection);
            dbQuery.ExecuteNonQuery();

            dbConnection.Close();

            await ctx.RespondAsync("Table created.").ConfigureAwait(false);
        }

        [Command("droptable")]
        public async Task DeleteTable(CommandContext ctx)
        {
            var connectionString = await ClientUtilities.GetConnectionStringAsync();

            using var dbConnection = new NpgsqlConnection(connectionString);
            dbConnection.Open();

            using var dbQuery = new NpgsqlCommand("DROP TABLE IF EXISTS events", dbConnection);
            dbQuery.ExecuteNonQuery();

            dbConnection.Close();

            await ctx.RespondAsync("Table dropped.").ConfigureAwait(false);
        }        
    }
}
