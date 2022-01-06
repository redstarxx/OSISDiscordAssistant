using System;
using System.Linq;
using System.Threading;
using System.Globalization;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Collections.Generic;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Entities;
using Npgsql;
using Humanizer;
using OSISDiscordAssistant.Attributes;
using OSISDiscordAssistant.Models;
using OSISDiscordAssistant.Utilities;
using OSISDiscordAssistant.Enums;

namespace OSISDiscordAssistant.Commands
{
    class EventCommandsModule : BaseCommandModule
    {
        /// <summary>
        /// Commands to operate the Events Manager's create or list option.
        /// </summary>
        /// <param name="ctx">The respective context that the command belongs to.</param>
        /// <param name="operationSelection">Operation type to run.</param>
        [RequireMainGuild, RequireAccessRole]
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

                await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, enter your event name. You have one minute.");
                var interactivityModule = ctx.Client.GetInteractivity();
                var eventNameResult = await interactivityModule.WaitForMessageAsync
                    (x => x.Author.Id == ctx.User.Id && (x.Channel.Id == ctx.Channel.Id), TimeSpan.FromMinutes(1));

                if (!eventNameResult.TimedOut)
                {
                    // Checks whether the given event name matches with another event that has the exact name as given.
                    using (var db = new EventContext())
                    {                       
                        bool eventNameExists = false;

                        foreach (var events in db.Events)
                        {
                            if (events.EventName.ToLowerInvariant() == eventNameResult.Result.Content.ToLowerInvariant())
                            {
                                eventNameExists = true;

                                break;
                            }
                        }

                        if (eventNameExists)
                        {
                            string toSend = $"{Formatter.Bold("[ERROR]")} The event {Formatter.InlineCode(eventNameResult.Result.Content)} already exists! Try again with a different name.";

                            await ctx.Channel.SendMessageAsync(toSend);
                            
                            return;
                        }
                    }

                    eventName = eventNameResult.Result.Content;

                    if (eventName.Length > 50)
                    {
                        await ctx.Channel.SendMessageAsync("**[ERROR]** Maximum character limit of 50 characters exceeded. You must re-run the command to finish.");
                        return;
                    }

                    await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, tag the person-in-charge (ketua / wakil ketua acara) for {Formatter.Bold(eventNameResult.Result.Content)}. You have one minute.");
                    var personInChargeResult = await interactivityModule.WaitForMessageAsync
                        (x => x.Author.Id == ctx.User.Id && x.Channel.Id == ctx.Channel.Id, TimeSpan.FromMinutes(1));

                    if (!personInChargeResult.TimedOut)
                    {
                        personInCharge = personInChargeResult.Result.Content;
                        if (eventName.Length > 100)
                        {
                            await ctx.Channel.SendMessageAsync("**[ERROR]** Maximum character limit of 100 characters exceeded. You must re-run the command to finish.");
                            return;
                        }

                        await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, enter your event date for {Formatter.Bold(eventNameResult.Result.Content)}. You have one minute.");
                        var eventDateResult = await interactivityModule.WaitForMessageAsync
                            (x => x.Author.Id == ctx.User.Id && x.Channel.Id == ctx.Channel.Id, TimeSpan.FromMinutes(1));

                        if (!eventDateResult.TimedOut)
                        {
                            eventDate = eventDateResult.Result.Content;
                            if (eventDate.Length > 50)
                            {
                                await ctx.Channel.SendMessageAsync("**[ERROR]** Maximum character limit of 50 characters exceeded. You must re-run the command to finish.");
                                return;
                            }

                            // Checks whether the provided date specifies the year. This assumes that there are no numbers up to 4 digits occuring more than once.
                            Regex regex = new Regex(@"\d{4}");

                            Match yearExist = regex.Match(eventDate);
                            if (!yearExist.Success)
                            {
                                ulong eventDateMessageId = eventDateResult.Result.Id;
                                var toReply = await eventDateResult.Result.Channel.GetMessageAsync(eventDateMessageId);

                                await toReply.RespondAsync($"{Formatter.Bold("[ERROR]")} Oops! It looks like you did not include the year of the event. Please add it! (example: 25 Juni 2021).");

                                return;
                            }

                            // The following try-catch blocks will attempt to parse the given date time. 
                            // If it fails, the event creation is canceled as it would not allow the bot to parse them for event reminders.
                            try
                            {
                                var cultureInfoUS = new CultureInfo("en-US");

                                // Add 7 hours ahead because for some reason Linux doesn't pick the user preferred timezone.
                                DateTime currentTime = DateTime.Now;

                                DateTime toConvert = DateTime.Parse(eventDate, cultureInfoUS);

                                TimeSpan calculateTimeSpan = toConvert - currentTime;

                                if (calculateTimeSpan.TotalDays > 365)
                                {
                                    string errorMessage = "**[ERROR]** Maximum allowed time span is one year (365 days). Alternatively, include the year of the event as well if you have not.";
                                    await ctx.RespondAsync(errorMessage);

                                    return;
                                }

                                if (calculateTimeSpan.Days < 1)
                                {
                                    string errorMessage = "**[ERROR]** Minimum allowed date is one day before the event. Alternatively, include the year of the event as well if you have not.";
                                    await ctx.Channel.SendMessageAsync(errorMessage);

                                    return;
                                }

                                // Set the culture info to store.
                                eventDateCultureInfo = "en-US";
                            }

                            catch
                            {
                                try
                                {
                                    var cultureInfoID = new CultureInfo("id-ID");

                                    // Add 7 hours ahead because for some reason Linux doesn't pick the user preferred timezone.
                                    DateTime currentTime = DateTime.Now;

                                    DateTime toConvert = DateTime.Parse(eventDate, cultureInfoID);

                                    TimeSpan calculateTimeSpan = toConvert - currentTime;

                                    if (calculateTimeSpan.TotalDays > 365)
                                    {
                                        string errorMessage = "**[ERROR]** Maximum allowed time span is one year (365 days). Alternatively, include the year of the event as well if you have not.";
                                        await ctx.RespondAsync(errorMessage);

                                        return;
                                    }

                                    if (calculateTimeSpan.Days < 1)
                                    {
                                        string errorMessage = "**[ERROR]** Minimum allowed date is one day before the event. Alternatively, include the year of the event as well if you have not.";
                                        await ctx.Channel.SendMessageAsync(errorMessage);

                                        return;
                                    }

                                    // Set the culture info to store.
                                    eventDateCultureInfo = "id-ID";
                                }

                                catch
                                {
                                    // Notify the user that the provided event date cannot be parsed.
                                    string errorMessage = $"{Formatter.Bold("[ERROR]")} An error occured. Your event date cannot be parsed. Make sure your date and time is written in English or Indonesian. Example: 25 June 2021.";
                                    await ctx.Channel.SendMessageAsync(errorMessage);

                                    return;
                                }
                            }

                            await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, enter your event description for {Formatter.Bold(eventNameResult.Result.Content)}. You have one minute.");
                            var eventDescriptionResult = await interactivityModule.WaitForMessageAsync
                                (x => x.Author.Id == ctx.User.Id && x.Channel.Id == ctx.Channel.Id, TimeSpan.FromMinutes(1));

                            if (!eventDescriptionResult.TimedOut)
                            {
                                eventDescription = eventDescriptionResult.Result.Content;
                                if (eventDescription.Length > 255)
                                {
                                    await ctx.Channel.SendMessageAsync("**[ERROR]** Maximum character limit of 255 characters exceeded. You must re-run the command to finish.");
                                    return;
                                }
                            }

                            else
                            {
                                await ctx.Channel.SendMessageAsync("**[TIMED OUT]** Event description not entered within given time span. Re-run the command if you still need to create your event.");
                                return;
                            }
                        }

                        else
                        {
                            await ctx.Channel.SendMessageAsync("**[TIMED OUT]** Event date not entered within given time span. Re-run the command if you still need to create your event.");
                            
                            return;
                        }
                    }

                    else
                    {
                        await ctx.Channel.SendMessageAsync("**[TIMED OUT]** Person-in-charge not entered within given time span. Re-run the command if you still need to create your event.");
                        return;
                    }
                }

                else
                {
                    await ctx.Channel.SendMessageAsync("**[TIMED OUT]** Event name not entered within given time span. Re-run the command if you still need to create your event.");
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

                    await ctx.Channel.SendMessageAsync($"Okay {ctx.Member.Mention}, your event, {Formatter.Bold(eventName)} has been created.");
                });
            }

            else if (operationSelection == "list")
            {
                var embedBuilder = new DiscordEmbedBuilder
                {
                    Title = "Events Manager - Listing All Events...",
                    Timestamp = DateTime.Now,
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = "OSIS Discord Assistant"
                    },
                    Color = DiscordColor.MidnightBlue
                };

                var notifyMessage = await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[EVENTS MANAGER]")} Give me a second to process everything...");
                await ctx.TriggerTypingAsync();

                Task offloadToTask = Task.Run(async () =>
                {
                    int counter = 0;

                    List<DiscordEmbedBuilder> eventEmbeds = new List<DiscordEmbedBuilder>();

                    using (var db = new EventContext())
                    {
                        foreach (var events in db.Events)
                        {
                            DateTime eventDate = DateTime.Parse(events.EventDate, new CultureInfo(events.EventDateCultureInfo));

                            if (DateTime.Now.Year == eventDate.Year)
                            {
                                var embedBuilder = new DiscordEmbedBuilder
                                {
                                    Title = "Events Manager - Listing All Events...",
                                    Description = "To navigate around the search results, interact with the buttons below, if any.",
                                    Timestamp = DateTime.Now,
                                    Footer = new DiscordEmbedBuilder.EmbedFooter
                                    {
                                        Text = "OSIS Discord Assistant"
                                    },
                                    Color = DiscordColor.MidnightBlue
                                };

                                embedBuilder.AddField($"(ID: {events.Id}) {events.EventName} [{events.EventDate}]", ComposeEventDescriptionField(events, eventDate), true);

                                eventEmbeds.Add(embedBuilder);
                                counter++;
                            }
                        }
                    }

                    await notifyMessage.DeleteAsync();

                    if (counter == 0)
                    {
                        embedBuilder.Description = $"There are no events registered for the year {Formatter.Underline(DateTime.Now.Year.ToString())}.";

                        await ctx.Channel.SendMessageAsync(embed: embedBuilder);
                    }

                    else if (counter == 1)
                    {
                        var messageBuilder = new DiscordMessageBuilder();

                        messageBuilder.WithContent($"List of all registered events for the year {Formatter.Underline(DateTime.Now.Year.ToString())}. Indexed {counter} ({counter.ToWords()}) events.")
                                      .WithEmbed(eventEmbeds.First().WithDescription(string.Empty));

                        await ctx.Channel.SendMessageAsync(builder: messageBuilder);
                    }

                    else
                    {
                        var pga = eventEmbeds.Select(x => new Page($"List of all registered events for the year {Formatter.Underline(DateTime.Now.Year.ToString())}. Indexed {counter} ({counter.ToWords()}) events.", x)).ToArray();

                        var interactivity = ctx.Client.GetInteractivity();
                        await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pga, PaginationBehaviour.WrapAround, ButtonPaginationBehavior.Disable);
                    }
                });
            }

            else if (operationSelection == "update")
            {
                string toSend = $"{Formatter.Bold("[SYNTAX]")} !event update [EVENT ID or EVENT NAME]\nExample: {Formatter.InlineCode("!event update LDKS 2021")}";
                await ctx.Channel.SendMessageAsync(toSend);
            }

            else if (operationSelection == "get")
            {
                string toSend = $"{Formatter.Bold("[SYNTAX]")} !event get [EVENT ID or EVENT NAME]\nExample: {Formatter.InlineCode("!event get LDKS 2021")}";
                await ctx.Channel.SendMessageAsync(toSend);
            }

            else if (operationSelection == "search")
            {
                string toSend = $"{Formatter.Bold("[SYNTAX]")} !event search [EVENT NAME]\nExample: {Formatter.InlineCode("!event search LDKS 2021")}";
                await ctx.Channel.SendMessageAsync(toSend);
            }

            else if (operationSelection == "delete")
            {
                string toSend = $"{Formatter.Bold("[SYNTAX]")} !event delete [EVENT ID or EVENT NAME]\nExample: {Formatter.InlineCode("!event delete LDKS 2021")}";
                await ctx.Channel.SendMessageAsync(toSend);
            }

            else if (operationSelection == "proposal")
            {
                string toSend = $"{Formatter.Bold("[SYNTAX]")} !event proposal [EVENT ID or EVENT NAME]\nExample: {Formatter.InlineCode("!event proposal LDKS 2021")}";
                await ctx.Channel.SendMessageAsync(toSend);
            }

            else
            {
                await SendHelpEmoji(ctx, operationSelection);
            }
        }

        /// <summary>
        /// Commands to operate the Events Manager's update or delete or search option.
        /// </summary>
        /// <param name="ctx">The respective context that the command belongs to.</param>
        /// <param name="operationSelection">Operation type to run.</param>
        /// <param name="optionalInput">Row number or event name from the events table to update or delete or search. Optional.</param>
        [RequireMainGuild, RequireAccessRole]
        [Command("event")]
        public async Task Event(CommandContext ctx, string operationSelection, params string[] optionalInput)
        {
            if (operationSelection == "create")
            {
                await ctx.Channel.SendMessageAsync($"Currently, OSIS does not support creating an event by directly calling the command. Perhaps in the future!\nTo receive automated reminders for your event, type {Formatter.InlineCode("!event create")} and enter the questioned details.");

                return;
            }

            else if (operationSelection == "update")
            {
                var embedBuilder = new DiscordEmbedBuilder
                {
                    Timestamp = DateTime.Now,
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = "OSIS Discord Assistant"
                    },
                    Color = DiscordColor.MidnightBlue
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

                            if (!rowExists)
                            {
                                string errorMessage = $"{Formatter.Bold("[ERROR]")} An error occured. You must provide the correct ID or name of the event you are updating. Refer to {Formatter.InlineCode("!event list")} or {Formatter.InlineCode("!event search")}.";
                                await ctx.Channel.SendMessageAsync(errorMessage);

                                return;
                            }

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
                                await ctx.Channel.SendMessageAsync(errorMessage);

                                return;
                            }
                        }

                        // Checks whether the selected event has already expired.
                        bool hasExpired = db.Events.SingleOrDefault(x => x.Id == rowID).Expired;

                        if (rowExists)
                        {
                            string previousEventName = db.Events.SingleOrDefault(x => x.Id == rowID).EventName;

                            embedBuilder.Title = $"Events Manager - Updating {previousEventName}...";
                            embedBuilder.Description = $"Choose either one of the following emojis to select what are you going to change from {Formatter.Bold(previousEventName)}.\n\n" +
                                "**[1]** Change event name;\n**[2]** Change event person-in-charge (ketua / wakil ketua acara);\n**[3]** Change event date and time;\n**[4]** Change event description.\n\n" +
                                $"You have 5 (five) minutes to make your choice otherwise the bot will abort. To cancel your changes, type {Formatter.InlineCode("abort")}.";
                            var updateEmbed = await ctx.Channel.SendMessageAsync(embed: embedBuilder);

                            var numberOneEmoji = DiscordEmoji.FromName(ctx.Client, ":one:");
                            var numberTwoEmoji = DiscordEmoji.FromName(ctx.Client, ":two:");
                            var numberThreeEmoji = DiscordEmoji.FromName(ctx.Client, ":three:");
                            var numberFourEmoji = DiscordEmoji.FromName(ctx.Client, ":four:");

                            await updateEmbed.CreateReactionAsync(numberOneEmoji);
                            await updateEmbed.CreateReactionAsync(numberTwoEmoji);
                            await updateEmbed.CreateReactionAsync(numberThreeEmoji);
                            await updateEmbed.CreateReactionAsync(numberFourEmoji);

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

                                    var updateNameMessage = await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, enter your new event name for {Formatter.Bold(previousEventName)}. You have one minute.");
                                    var eventNameResult = await inputInteractivity.WaitForMessageAsync
                                        (x => x.Author.Id == ctx.User.Id && x.Channel.Id == ctx.Channel.Id, TimeSpan.FromMinutes(1));

                                    if (!eventNameResult.TimedOut)
                                    {
                                        // Checks whether the given event name matches with another event that has the exact name as given.
                                        bool eventNameExists = false;

                                        foreach (var events in db.Events)
                                        {
                                            if (events.EventName.ToLowerInvariant() == eventNameResult.Result.Content.ToLowerInvariant())
                                            {
                                                eventNameExists = true;

                                                break;
                                            }
                                        }

                                        if (eventNameExists)
                                        {
                                            string toSend = $"{Formatter.Bold("[ERROR]")} The event {Formatter.InlineCode(eventNameResult.Result.Content)} already exists! Try again with a different name.";

                                            await ctx.Channel.SendMessageAsync(toSend);

                                            return;
                                        }

                                        eventName = eventNameResult.Result.Content;

                                        if (eventName.Length > 50)
                                        {
                                            await ctx.Channel.SendMessageAsync("**[ERROR]** Operation aborted. Maximum character limit of 50 characters exceeded.");
                                            return;
                                        }

                                        if (eventName == "abort")
                                        {
                                            await ctx.Channel.SendMessageAsync($"Update operation aborted as per your request, {ctx.Member.Mention}.");
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

                                            await ctx.Channel.SendMessageAsync(embed: embedBuilder);
                                        });
                                    }

                                    else
                                    {
                                        await ctx.Channel.SendMessageAsync("**[TIMED OUT]** Operation aborted. Event name not entered within given time span.");
                                        return;
                                    }
                                }

                                else if (selectionResult.Result.Emoji == numberTwoEmoji)
                                {
                                    await updateEmbed.DeleteAllReactionsAsync();

                                    string previousPersonInCharge = db.Events.SingleOrDefault(x => x.Id == rowID).PersonInCharge;

                                    var inputInteractivity = ctx.Client.GetInteractivity();

                                    var updatePersonInChargeMessage = await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, enter your new person-in-charge for {Formatter.Bold(previousEventName)}. You have one minute.");
                                    var eventPersonInChargeResult = await inputInteractivity.WaitForMessageAsync
                                        (x => x.Author.Id == ctx.User.Id && x.Channel.Id == ctx.Channel.Id, TimeSpan.FromMinutes(1));

                                    if (!eventPersonInChargeResult.TimedOut)
                                    {
                                        personInCharge = eventPersonInChargeResult.Result.Content;
                                        if (eventName.Length > 100)
                                        {
                                            await ctx.Channel.SendMessageAsync("**[ERROR]** Operation aborted. Maximum character limit of 100 characters exceeded.");
                                            return;
                                        }

                                        if (eventName == "abort")
                                        {
                                            await ctx.Channel.SendMessageAsync($"Update operation aborted as per your request, {ctx.Member.Mention}.");
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

                                            await ctx.Channel.SendMessageAsync(embed: embedBuilder);
                                        });                                       
                                    }

                                    else
                                    {
                                        await ctx.Channel.SendMessageAsync("**[TIMED OUT]** Operation aborted. Event name not entered within given time span.");
                                        return;
                                    }
                                }

                                else if (selectionResult.Result.Emoji == numberThreeEmoji)
                                {
                                    await updateEmbed.DeleteAllReactionsAsync();

                                    string previousEventDate = db.Events.SingleOrDefault(x => x.Id == rowID).EventDate;

                                    var inputInteractivity = ctx.Client.GetInteractivity();

                                    var updateDateMessage = await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, enter your new event date for {Formatter.Bold(previousEventName)}. You have one minute.");
                                    var eventDateResult = await inputInteractivity.WaitForMessageAsync
                                        (x => x.Author.Id == ctx.User.Id && x.Channel.Id == ctx.Channel.Id, TimeSpan.FromMinutes(1));

                                    if (!eventDateResult.TimedOut)
                                    {
                                        eventDate = eventDateResult.Result.Content;
                                        if (eventDate.Length > 50)
                                        {
                                            await ctx.Channel.SendMessageAsync("**[ERROR]** Operation aborted. Maximum character limit of 50 characters exceeded.");
                                            return;
                                        }

                                        // Checks whether the provided date specifies the year. This assumes that there are no numbers up to 4 digits occuring more than once.
                                        Regex regex = new Regex(@"\d{4}");

                                        Match yearExist = regex.Match(eventDate);
                                        if (!yearExist.Success)
                                        {
                                            ulong eventDateMessageId = eventDateResult.Result.Id;
                                            var toReply = await eventDateResult.Result.Channel.GetMessageAsync(eventDateMessageId);

                                            await toReply.RespondAsync($"{Formatter.Bold("[ERROR]")} Oops! It looks like you did not include the year of the event. Please add it! (example: 25 Juni 2021).");

                                            return;
                                        }

                                        if (eventDate == "abort")
                                        {
                                            await ctx.Channel.SendMessageAsync($"Update operation aborted as per your request, {ctx.Member.Mention}.");
                                            return;
                                        }

                                        // The following try-catch blocks will attempt to parse the given date time.
                                        // If it fails, the event creation is canceled as it would not allow the bot to parse them for event reminders.
                                        try
                                        {
                                            var cultureInfoUS = new CultureInfo("en-US");

                                            // Add 7 hours ahead because for some reason Linux doesn't pick the user preferred timezone.
                                            DateTime currentTime = DateTime.Now;

                                            DateTime toConvert = DateTime.Parse(eventDate, cultureInfoUS);

                                            TimeSpan calculateTimeSpan = toConvert - currentTime;

                                            if (calculateTimeSpan.TotalDays > 365)
                                            {
                                                string errorMessage = "**[ERROR]** Maximum allowed time span is one year (365 days). Alternatively, include the year of the event as well if you have not.";
                                                await ctx.RespondAsync(errorMessage);

                                                return;
                                            }

                                            if (calculateTimeSpan.Days < 1)
                                            {
                                                string errorMessage = "**[ERROR]** Minimum allowed date is one day before the event. Alternatively, include the year of the event as well if you have not.";
                                                await ctx.Channel.SendMessageAsync(errorMessage);

                                                return;
                                            }

                                            // Set the culture info to store.
                                            eventDateCultureInfo = "en-US";
                                        }

                                        catch
                                        {
                                            try
                                            {
                                                var cultureInfoID = new CultureInfo("id-ID");

                                                // Add 7 hours ahead because for some reason Linux doesn't pick the user preferred timezone.
                                                DateTime currentTime = DateTime.Now;

                                                DateTime toConvert = DateTime.Parse(eventDate, cultureInfoID);

                                                TimeSpan calculateTimeSpan = toConvert - currentTime;

                                                if (calculateTimeSpan.TotalDays > 365)
                                                {
                                                    string errorMessage = "**[ERROR]** Maximum allowed time span is one year (365 days). Alternatively, include the year of the event as well if you have not.";
                                                    await ctx.RespondAsync(errorMessage);

                                                    return;
                                                }

                                                if (calculateTimeSpan.Days < 1)
                                                {
                                                    string errorMessage = "**[ERROR]** Minimum allowed date is one day before the event. Alternatively, include the year of the event as well if you have not.";
                                                    await ctx.Channel.SendMessageAsync(errorMessage);

                                                    return;
                                                }

                                                // Set the culture info to store.
                                                eventDateCultureInfo = "id-ID";
                                            }

                                            catch
                                            {
                                                // Notify the user that the provided event date cannot be parsed.
                                                string errorMessage = $"{Formatter.Bold("[ERROR]")} An error occured. Your event date cannot be parsed. Make sure your date and time is written in English or Indonesian. Example: 25 June 2021.";
                                                await ctx.Channel.SendMessageAsync(errorMessage);

                                                return;
                                            }
                                        }

                                        var warningMessage = await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[WARNING]")} By updating the date of {Formatter.Bold(previousEventName)}, it will reset the reminder for this event and all members may be pinged again to remind them if a reminder for this event was sent previously. Proceed?");

                                        DiscordEmoji checkMarkEmoji = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");
                                        DiscordEmoji crossMarkEmoji = DiscordEmoji.FromName(ctx.Client, ":x:");

                                        await warningMessage.CreateReactionAsync(checkMarkEmoji);
                                        await warningMessage.CreateReactionAsync(crossMarkEmoji);

                                        var warningInteractivityResult = await ctx.Client.GetInteractivity().WaitForReactionAsync
                                            (x => x.Message == warningMessage && (x.User.Id == ctx.User.Id) && (x.Emoji == checkMarkEmoji || x.Emoji == crossMarkEmoji), TimeSpan.FromMinutes(15));

                                        if (!warningInteractivityResult.TimedOut)
                                        {
                                            if (warningInteractivityResult.Result.Emoji == checkMarkEmoji)
                                            {
                                                await ctx.Channel.SendMessageAsync("Okay.");
                                            }

                                            else if (warningInteractivityResult.Result.Emoji == crossMarkEmoji)
                                            {
                                                await ctx.Channel.SendMessageAsync($"Cancellation acknowledged. Aborted updating {Formatter.Bold(previousEventName)}.");

                                                return;
                                            }
                                        }

                                        else
                                        {
                                            await ctx.RespondAsync($"You're taking too long to react. Feel free to retry updating {Formatter.Bold(previousEventName)} again.");

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
                                                    rowToUpdate.EventDate = eventDate;
                                                    rowToUpdate.EventDateCultureInfo = eventDateCultureInfo;
                                                    rowToUpdate.PreviouslyReminded = false;
                                                    rowToUpdate.Expired = false;
                                                    rowToUpdate.ProposalReminded = false;

                                                    dbUpdate.SaveChanges();

                                                    await dbUpdate.DisposeAsync();
                                                }

                                                embedBuilder.Title = $"Events Manager - {previousEventName} Update Details";
                                                embedBuilder.Description = $"{ctx.Member.Mention} has made update(s) to {previousEventName}.\n\n{Formatter.Bold("Changes made:")}\n• Changed event date from {Formatter.InlineCode(previousEventDate)} to {Formatter.InlineCode(eventDate)}.";
                                                embedBuilder.Timestamp = DateTime.Now;

                                                await ctx.Channel.SendMessageAsync(embed: embedBuilder);
                                            }
                                        });                                       
                                    }

                                    else
                                    {
                                        await ctx.Channel.SendMessageAsync("**[TIMED OUT]** Operation aborted. Event date not entered within given time span.");
                                        return;
                                    }
                                }

                                else if (selectionResult.Result.Emoji == numberFourEmoji)
                                {
                                    await updateEmbed.DeleteAllReactionsAsync();

                                    string previousEventDescription = db.Events.SingleOrDefault(x => x.Id == rowID).EventDescription;

                                    var inputInteractivity = ctx.Client.GetInteractivity();

                                    var updateDescriptionMessage = await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, enter your new event description for {Formatter.Bold(previousEventName)}. You have one minute.");
                                    var eventDescriptionResult = await inputInteractivity.WaitForMessageAsync
                                        (x => x.Author.Id == ctx.User.Id && x.Channel.Id == ctx.Channel.Id, TimeSpan.FromMinutes(1));

                                    if (!eventDescriptionResult.TimedOut)
                                    {
                                        eventDescription = eventDescriptionResult.Result.Content;
                                        if (eventDescription.Length > 254)
                                        {
                                            await ctx.Channel.SendMessageAsync("**[ERROR]** Operation aborted. Maximum character limit of 255 characters exceeded.");
                                            return;
                                        }

                                        if (eventDescription == "abort")
                                        {
                                            await ctx.Channel.SendMessageAsync($"Update operation aborted as per your request, {ctx.Member.Mention}.");
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

                                                await ctx.Channel.SendMessageAsync(embed: embedBuilder);
                                            }
                                        });
                                    }

                                    else
                                    {
                                        await ctx.Channel.SendMessageAsync("**[TIMED OUT]** Operation aborted. Event date not entered within given time span.");
                                        return;
                                    }
                                }
                            }

                            else
                            {
                                await ctx.Channel.SendMessageAsync($"**[TIMED OUT]** Update selection for {previousEventName} not selected within given time span. Re-run the command if you still need to update your event.");
                            }
                        }

                        else
                        {
                            embedBuilder.Title = "Events Manager - Update Error";
                            embedBuilder.Description = $"Event ID {Formatter.Bold(rowID.ToString())} does not exist. Make sure you are selecting an event ID from Events Manager - List.";
                            await ctx.Channel.SendMessageAsync(embed: embedBuilder);
                        }

                    }
                }

                catch (PostgresException ex)
                {
                    embedBuilder.Title = "Events Manager - Update Error";
                    embedBuilder.Description = $"An error occured while working with the database. Exception details: {ex.Message}.";
                    await ctx.RespondAsync(embed: embedBuilder);
                }
            }

            else if (operationSelection == "delete")
            {
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
                            try
                            {
                                rowExists = db.Events.Any(x => x.Id == rowIDRaw);
                                rowID = db.Events.SingleOrDefault(x => x.Id == rowIDRaw).Id;
                            }

                            catch
                            {
                                string errorMessage = $"{Formatter.Bold("[ERROR]")} An error occured. You must provide the correct ID of the event you are updating. Refer to {Formatter.InlineCode("!event list")} or {Formatter.InlineCode("!event search")}.";
                                await ctx.Channel.SendMessageAsync(errorMessage);

                                return;
                            }
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
                                string errorMessage = $"{Formatter.Bold("[ERROR]")} An error occured. You must provide the correct name of the event you are updating. Refer to {Formatter.InlineCode("!event list")} or {Formatter.InlineCode("!event search")}.";
                                await ctx.Channel.SendMessageAsync(errorMessage);

                                return;
                            }
                        }

                        // Checks whether the selected event is already expired
                        bool hasExpired = db.Events.SingleOrDefault(x => x.Id == rowID).Expired;

                        if (hasExpired)
                        {
                            string errorMessage = $"{Formatter.Bold("[ERROR]")} Oops! You can only delete events that has not yet expired.";
                            await ctx.Channel.SendMessageAsync(errorMessage);

                            return;
                        }

                        if (rowExists)
                        {
                            var rowToDelete = db.Events.SingleOrDefault(x => x.Id == rowID);
                            var eventNameQuery = db.Events.SingleOrDefault(x => x.Id == rowID).EventName;

                            db.Remove(rowToDelete);
                            db.SaveChanges();

                            await ctx.Channel.SendMessageAsync($"{Formatter.Bold(eventNameQuery.ToString())} (Event ID: {rowID.ToString()}) successfully deleted from Events Manager.");
                        }

                        else
                        {
                            return;
                        }                        
                    }
                }

                catch (Exception ex)
                {
                    await ctx.Channel.SendMessageAsync($"An error occured while working with the database.\nException details: {ex.Message}.");
                }
            }

            else if (operationSelection == "search")
            {
                string parseOptionalInput = string.Join(" ", optionalInput);

                var embedBuilder = new DiscordEmbedBuilder
                {
                    Title = "Events Manager - Search Result",
                    Timestamp = DateTime.Now,
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = "OSIS Discord Assistant"
                    },
                    Color = DiscordColor.MidnightBlue
                };

                try
                {
                    string toSearch = parseOptionalInput.ToLowerInvariant();

                    using (var db = new EventContext())
                    {
                        try
                        {
                            int counter = 0;

                            int additionalCounter = 0;

                            foreach (var events in db.Events)
                            {
                                if (events.EventName.ToLowerInvariant().Contains(toSearch))
                                {
                                    if (counter > 25)
                                    {
                                        additionalCounter++;
                                    }

                                    else
                                    {
                                        DateTime eventDate = DateTime.Parse(events.EventDate, new CultureInfo(events.EventDateCultureInfo));

                                        embedBuilder.AddField($"(ID: {events.Id}) {events.EventName} [{events.EventDate}]", ComposeEventDescriptionField(events, eventDate), true);

                                        counter++;
                                    }
                                }
                            }

                            if (counter == 0)
                            {
                                embedBuilder.Description = $"Oops! There are no results for keyword {Formatter.InlineCode(parseOptionalInput)}! If you are getting an event by ID, use {Formatter.InlineCode("!event get")}.";
                            }

                            else
                            {
                                int searchResultCount = counter + additionalCounter;

                                embedBuilder.Description = $"Showing {counter} ({counter.ToWords()}) out of {searchResultCount} ({searchResultCount.ToWords()}) query result for keyword {Formatter.InlineCode(parseOptionalInput)}...";
                            }

                            await ctx.Channel.SendMessageAsync(embed: embedBuilder);
                        }

                        catch
                        {
                            embedBuilder.Description = $"Oops! There are no results for keyword {Formatter.InlineCode(parseOptionalInput)}! Did you typed the correct event name?";
                            await ctx.Channel.SendMessageAsync(embed: embedBuilder);

                            return;
                        }
                    }
                }

                catch (PostgresException ex)
                {
                    embedBuilder.ClearFields();

                    embedBuilder.Title = "Events Manager - Error";
                    embedBuilder.Description = $"An error occured while working with the database.\nException details: {ex.Message}.";

                    await ctx.Channel.SendMessageAsync(embed: embedBuilder);
                }

                catch (Exception ex)
                {
                    embedBuilder.ClearFields();

                    embedBuilder.Title = "Events Manager - Error";
                    embedBuilder.Description = $"An error occured while working with the database.\nException details: {ex.Message}.";

                    await ctx.Channel.SendMessageAsync(embed: embedBuilder);
                }                             
            }

            else if (operationSelection == "get")
            {
                string parseOptionalInput = string.Join(" ", optionalInput);

                // Converts the optionalInput into a number so we can access our targeted row.
                // If true, execute query based on row ID. If false, execute query based on provided event name.
                bool isNumber = int.TryParse(parseOptionalInput, out int rowID);

                bool isLetter = parseOptionalInput.All(char.IsLetter);

                var embedBuilder = new DiscordEmbedBuilder
                {
                    Title = "Events Manager - Get Result",
                    Timestamp = DateTime.Now,
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = "OSIS Discord Assistant"
                    },
                    Color = DiscordColor.MidnightBlue
                };

                if (isNumber)
                {
                    using (var db = new EventContext())
                    {
                        Events rowToRead = null;

                        try
                        {
                            rowToRead = db.Events.SingleOrDefault(x => x.Id == rowID);

                            embedBuilder.Description = $"Showing result for event ID {Formatter.InlineCode(rowID.ToString())}...";

                            DateTime eventDate = DateTime.Parse(rowToRead.EventDate, new CultureInfo(rowToRead.EventDateCultureInfo));

                            embedBuilder.AddField($"(ID: {rowToRead.Id}) {rowToRead.EventName} [{rowToRead.EventDate}]", ComposeEventDescriptionField(rowToRead, eventDate), true);

                            await ctx.Channel.SendMessageAsync(embed: embedBuilder);
                        }

                        catch
                        {
                            embedBuilder.Description = $"Oops! There are no results for event ID {Formatter.InlineCode(rowID.ToString())}! Have you specified the correct event ID? Alternatively, use {Formatter.InlineCode("!event search")} with the event name you are looking for.";
                            await ctx.Channel.SendMessageAsync(embed: embedBuilder);

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
                            bool eventFound = false;

                            foreach (var events in db.Events)
                            {
                                if (events.EventName.ToLowerInvariant() == toSearch)
                                {
                                    DateTime eventDate = DateTime.Parse(events.EventDate, new CultureInfo(events.EventDateCultureInfo));

                                    embedBuilder.AddField($"(ID: {events.Id}) {events.EventName} [{events.EventDate}]", ComposeEventDescriptionField(events, eventDate), true);

                                    eventFound = true;

                                    break;
                                }
                            }

                            if (eventFound is false)
                            {
                                embedBuilder.Description = $"Oops! There are no results for event name {Formatter.InlineCode(parseOptionalInput)}! Have you typed the exact event name you are looking for? Alternatively, use {Formatter.InlineCode("!event search")} with the event name you are looking for.";
                            }

                            else
                            {
                                embedBuilder.Description = $"Showing query result for event name {Formatter.InlineCode(parseOptionalInput)}...";
                            }

                            await ctx.Channel.SendMessageAsync(embed: embedBuilder);
                        }

                        catch
                        {
                            embedBuilder.Description = $"Oops! There are no results for event name {Formatter.InlineCode(parseOptionalInput)}! Have you typed the exact event name you are looking for? Alternatively, use {Formatter.InlineCode("!event search")} with the event name you are looking for.";
                            await ctx.Channel.SendMessageAsync(embed: embedBuilder);

                            return;
                        }
                    }
                }
            }

            else if (operationSelection == "proposal")
            {
                string accessingEventName = string.Join(" ", optionalInput);

                string eventName = null;

                int rowID = 0;

                using (var db = new EventContext())
                {
                    bool isNumber = int.TryParse(string.Join(" ", optionalInput), out int rowIDRaw);
                    bool rowExists = false;

                    string inputEventName = string.Join(" ", optionalInput);

                    if (isNumber)
                    {
                        rowExists = db.Events.Any(x => x.Id == rowIDRaw);

                        if (!rowExists)
                        {
                            string errorMessage = $"{Formatter.Bold("[ERROR]")} An error occured. You must provide the correct ID or name of the event you are updating. Refer to {Formatter.InlineCode("!event list")} or {Formatter.InlineCode("!event search")}.";
                            await ctx.Channel.SendMessageAsync(errorMessage);

                            return;
                        }

                        rowID = db.Events.SingleOrDefault(x => x.Id == rowIDRaw).Id;
                        eventName = db.Events.SingleOrDefault(x => x.Id == rowIDRaw).EventName;
                    }

                    else if (!isNumber)
                    {
                        int counter = 0;

                        foreach (var events in db.Events)
                        {
                            if (events.EventName.ToLowerInvariant() == inputEventName.ToLowerInvariant())
                            {
                                rowID = events.Id;
                                eventName = events.EventName;
                                rowExists = true;
                                counter++;

                                break;
                            }
                        }

                        if (counter == 0)
                        {
                            string errorMessage = $"{Formatter.Bold("[ERROR]")} An error occured. You must provide the correct ID or name of the event you are updating. Refer to {Formatter.InlineCode("!event list")} or {Formatter.InlineCode("!event search")}.";
                            await ctx.Channel.SendMessageAsync(errorMessage);

                            return;
                        }
                    }
                }

                var embedBuilder = new DiscordEmbedBuilder
                {
                    Timestamp = DateTime.Now,
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = "OSIS Discord Assistant"
                    },
                    Color = DiscordColor.MidnightBlue
                };

                string fileExist;

                using (var db = new EventContext())
                {
                    bool proposalExist = db.Events.SingleOrDefault(x => x.Id == rowID).ProposalFileTitle is null;

                    switch (proposalExist)
                    {
                        case true:
                            fileExist = "does not";
                            break;
                        case false:
                            fileExist = "does";
                            break;
                    }
                }

                embedBuilder.Title = $"Events Manager - Accessing {eventName}'s Proposal...";
                embedBuilder.Description = $"Choose either one of the following emojis to select what are you going to do with {Formatter.Bold(eventName)}. This event {fileExist} have a proposal file stored.\n\n" +
                    $"{Formatter.Bold("[1]")} Get the event's proposal document;\n{Formatter.Bold("[2]")} Store / update the event's proposal.\n{Formatter.Bold("[3]")} Delete the event's proposal.\n\n" +
                    $"You have 5 (five) minutes to select your choice.";
                var updateEmbed = await ctx.Channel.SendMessageAsync(embed: embedBuilder);

                var numberOneEmoji = DiscordEmoji.FromName(ctx.Client, ":one:");
                var numberTwoEmoji = DiscordEmoji.FromName(ctx.Client, ":two:");
                var numberThreeEmoji = DiscordEmoji.FromName(ctx.Client, ":three:");

                await updateEmbed.CreateReactionAsync(numberOneEmoji);
                await updateEmbed.CreateReactionAsync(numberTwoEmoji);
                await updateEmbed.CreateReactionAsync(numberThreeEmoji);

                var selectionInteractivity = ctx.Client.GetInteractivity();
                var selectionResult = await selectionInteractivity.WaitForReactionAsync
                    (x => x.Message == updateEmbed && (x.User.Id == ctx.User.Id) && (x.Emoji == numberOneEmoji || x.Emoji == numberTwoEmoji || x.Emoji == numberThreeEmoji), 
                    TimeSpan.FromMinutes(5));

                if (!selectionResult.TimedOut)
                {
                    await updateEmbed.DeleteAllReactionsAsync();

                    var messageBuilder = new DiscordMessageBuilder()
                    {
                        Content = $"Event {Formatter.Bold(eventName)}'s proposal is as follows:"
                    };

                    string fileTitle = null;

                    byte[] fileContent = null;

                    MemoryStream fileStream = new MemoryStream();

                    if (selectionResult.Result.Emoji == numberOneEmoji)
                    {
                        using (var db = new EventContext())
                        {
                            Events rowToAccess = null;
                            rowToAccess = db.Events.SingleOrDefault(x => x.Id == rowID);

                            fileTitle = rowToAccess.ProposalFileTitle;

                            fileContent = rowToAccess.ProposalFileContent;

                            if (fileTitle is null)
                            {
                                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} Event {Formatter.Bold(eventName)} does not have a proposal file stored!");

                                return;
                            }

                            fileStream = new MemoryStream(fileContent);

                            messageBuilder.WithFiles(new Dictionary<string, Stream>() { { fileTitle, fileStream } }, true);

                            await ctx.Channel.SendMessageAsync(builder: messageBuilder);
                        }
                    }

                    else if (selectionResult.Result.Emoji == numberTwoEmoji)
                    {
                        var interactivity = ctx.Client.GetInteractivity();

                        await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, drop / upload the proposal file here. An acceptable file is a Microsoft Word document. You have one minute!");

                        var proposalResult = await interactivity.WaitForMessageAsync(x => x.Author.Id == ctx.Member.Id, TimeSpan.FromMinutes(1));

                        if (!proposalResult.TimedOut)
                        {
                            if (proposalResult.Result.Content.Length is not 0)
                            {
                                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} You must be sending a file, not a message or anything else. Alternatively, avoid adding comments to the file you are uploading.");

                                return;
                            }

                            foreach (var attachment in proposalResult.Result.Attachments)
                            {
                                if (attachment.FileName.Length is 0)
                                {
                                    await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} Where is the file?");

                                    return;
                                }

                                if (!ClientUtilities.IsExtensionValid(attachment.FileName))
                                {
                                    await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} I'm sorry, the file is not valid! A valid proposal file must be a Microsoft Word file (.docx, .doc, .docm).");

                                    return;
                                }

                                WebClient wc = new WebClient();
                                fileTitle = attachment.FileName;
                                fileContent = wc.DownloadData(attachment.Url);
                                fileStream = new MemoryStream(fileContent);
                            }

                            string fileStatus = null;

                            using (var db = new EventContext())
                            {
                                Events rowToAccess = null;
                                rowToAccess = db.Events.SingleOrDefault(x => x.Id == rowID);

                                if (rowToAccess.ProposalFileTitle is null)
                                {
                                    fileStatus = "stored";
                                }

                                else
                                {
                                    fileStatus = "updated";
                                }

                                rowToAccess.ProposalFileTitle = fileTitle;

                                rowToAccess.ProposalFileContent = fileContent;

                                await db.SaveChangesAsync();
                            }

                            await ctx.Channel.SendMessageAsync($"Event {Formatter.Bold(eventName)}'s proposal document has been {fileStatus}!");
                        }

                        else
                        {
                            await ctx.RespondAsync($"{Formatter.Bold("[TIMED OUT]")} {ctx.Member.Mention} Proposal not uploaded within given time span. Re-run the command if you still need to update {Formatter.Bold(eventName)}'s proposal.");
                        }
                    }

                    else if (selectionResult.Result.Emoji == numberThreeEmoji)
                    {
                        using (var db = new EventContext())
                        {
                            Events rowToDelete = null;
                            rowToDelete = db.Events.SingleOrDefault(x => x.Id == rowID);

                            if (rowToDelete.ProposalFileTitle is null)
                            {
                                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} Event {Formatter.Bold(eventName)} does not have a proposal file stored!");

                                return;
                            }

                            rowToDelete.ProposalFileTitle = null;

                            rowToDelete.ProposalFileContent = null;

                            await db.SaveChangesAsync();
                        }

                        await ctx.Channel.SendMessageAsync($"Event {Formatter.Bold(eventName)}'s proposal document has been deleted!");
                    }

                    await fileStream.DisposeAsync();
                }

                else
                {
                    await ctx.RespondAsync($"{Formatter.Bold("[TIMED OUT]")} {ctx.Member.Mention} You did not choose an option within the given time span. Re-run the command if you still need to update {Formatter.Bold(eventName)}'s proposal.");
                }
            }

            else if (operationSelection == "list")
            {
                int year;

                var embedBuilder = new DiscordEmbedBuilder
                {
                    Title = "Events Manager - Listing All Events...",
                    Timestamp = DateTime.Now,
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = "OSIS Discord Assistant"
                    },
                    Color = DiscordColor.MidnightBlue
                };

                try
                {
                    year = Convert.ToInt32(string.Join(" ", optionalInput));
                }

                catch
                {
                    embedBuilder.Title = "Events Manager - Error";

                    embedBuilder.Description = $"I can only accept numbers (not dates!) and that number must be a year. Example: {Formatter.InlineCode("!event list 2019")}";

                    await ctx.Channel.SendMessageAsync(embedBuilder.Build());

                    return;
                }

                var notifyMessage = await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[EVENTS MANAGER]")} Give me a second to process everything...");
                await ctx.TriggerTypingAsync();

                Task offloadToTask = Task.Run(async () =>
                {
                    var eventEmbeds = new List<DiscordEmbedBuilder>();

                    int counter = 0;

                    using (var db = new EventContext())
                    {
                        foreach (var events in db.Events)
                        {
                            DateTime eventDate = DateTime.Parse(events.EventDate, new CultureInfo(events.EventDateCultureInfo));

                            if (year == eventDate.Year)
                            {
                                var embedBuilder = new DiscordEmbedBuilder
                                {
                                    Title = "Events Manager - Listing All Events...",
                                    Description = "To navigate around the search results, interact with the buttons below, if any.",
                                    Timestamp = DateTime.Now,
                                    Footer = new DiscordEmbedBuilder.EmbedFooter
                                    {
                                        Text = "OSIS Discord Assistant"
                                    },
                                    Color = DiscordColor.MidnightBlue
                                };

                                embedBuilder.AddField($"(ID: {events.Id}) {events.EventName} [{events.EventDate}]", ComposeEventDescriptionField(events, eventDate), true);

                                eventEmbeds.Add(embedBuilder);
                                counter++;
                            }
                        }
                    }

                    await notifyMessage.DeleteAsync();

                    if (counter == 0)
                    {
                        embedBuilder.Description = $"There are no events registered for the year {Formatter.Underline(year.ToString())}.";

                        await ctx.Channel.SendMessageAsync(embed: embedBuilder);
                    }

                    else if (counter == 1)
                    {
                        var messageBuilder = new DiscordMessageBuilder();

                        messageBuilder.WithContent($"List of all registered events for the year {Formatter.Underline(year.ToString())}. Indexed {counter} ({counter.ToWords()}) events.")
                                      .WithEmbed(eventEmbeds.First().WithDescription(string.Empty));

                        await ctx.Channel.SendMessageAsync(builder: messageBuilder);
                    }

                    else
                    {
                        var pga = eventEmbeds.Select(x => new Page($"List of all registered events for the year {Formatter.Underline(year.ToString())}. Indexed {counter} ({counter.ToWords()}) events.", x)).ToArray();

                        var interactivity = ctx.Client.GetInteractivity();
                        await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pga, PaginationBehaviour.WrapAround, ButtonPaginationBehavior.Disable);
                    }
                });
            }

            else
            {
                await SendHelpEmoji(ctx, operationSelection);
            }
        }

        /// <summary>
        /// Composes the embed field's value for the respective event.
        /// </summary>
        /// <returns>A string containing the details of the respective event.</returns>
        internal string ComposeEventDescriptionField(Events events, DateTime eventDate)
        {
            bool isProposalEmpty = events.ProposalFileTitle is null ? false : true;

            if (DateTime.Now < eventDate)
            {
                TimeSpan remainingDateTime = eventDate - DateTime.Now;

                return $"Status: {ClientUtilities.ConvertBoolValue(events.Expired, ConvertBoolOption.UpcomingOrDone)} ({Formatter.Timestamp(remainingDateTime, TimestampFormat.RelativeTime)})\nPerson-in-charge: {events.PersonInCharge}\nProposal: {ClientUtilities.ConvertBoolValue(isProposalEmpty, ConvertBoolOption.StoredOrNotStored)}\nDescription: {events.EventDescription}";
            }

            else
            {
                return $"Status: {ClientUtilities.ConvertBoolValue(events.Expired, ConvertBoolOption.UpcomingOrDone)}\nPerson-in-charge: {events.PersonInCharge}\nProposal: {ClientUtilities.ConvertBoolValue(isProposalEmpty, ConvertBoolOption.StoredOrNotStored)}\nDescription: {events.EventDescription}";
            }
        }

        /// <summary>
        /// Command to view the Events Manager commands and help.
        /// </summary>
        [RequireMainGuild, RequireAccessRole]
        [Command("event")]
        public async Task EventCreateOrList(CommandContext ctx)
        {
            await SendHelpEmbed(ctx);
        }
        
        internal async Task SendHelpEmbed(CommandContext ctx)
        {
            var embedBuilder = new DiscordEmbedBuilder
            {
                Title = "Events Manager - Overview",
                Timestamp = DateTime.Now,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "OSIS Discord Assistant"
                },
                Color = DiscordColor.MidnightBlue
            };

            embedBuilder.Description = "Events Manager integrates event planning, proposal submission reminder, and event execution reminder under one bot.\n\n" +
                $"{Formatter.Bold("!event create")} - Creates a new event.\n" +
                $"{Formatter.Bold("!event update")} - Updates an existing event.\n" +
                $"{Formatter.Bold("!event delete")} - Deletes an event.\n" +
                $"{Formatter.Bold("!event get")} - Gets an event directly with the provided name (must be exact) or ID.\n" +
                $"{Formatter.Bold("!event search")} - Search for an event which name contains the given keyword.\n" +
                $"{Formatter.Bold("!event proposal")} - Gets or updates the proposal file for the respective event name or ID.\n" +
                $"{Formatter.Bold("!event list")} - Lists all registered events for the year selected.\n";

            await ctx.Channel.SendMessageAsync(embed: embedBuilder);
        }

        internal async Task SendHelpEmoji(CommandContext ctx, string operationSelection)
        {
            var helpEmoji = DiscordEmoji.FromName(ctx.Client, ":sos:");
            string toSend = $"{Formatter.Bold("[ERROR]")} The option {Formatter.InlineCode(operationSelection)} does not exist! Type {Formatter.InlineCode("!event")} to list all options. Alternatively, click the emoji below to get help.";

            var errorMessage = await ctx.Channel.SendMessageAsync(toSend);

            await errorMessage.CreateReactionAsync(helpEmoji);

            var interactivity = ctx.Client.GetInteractivity();

            Thread.Sleep(TimeSpan.FromMilliseconds(500));
            var emojiResult = await interactivity.WaitForReactionAsync(x => x.Message == errorMessage && (x.Emoji == helpEmoji));

            if (emojiResult.Result.Emoji == helpEmoji)
            {
                await SendHelpEmbed(ctx);
            }
        }
    }
}
