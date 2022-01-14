using System;
using System.Linq;
using System.Threading;
using System.Globalization;
using System.Threading.Tasks;
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
using Humanizer;
using OSISDiscordAssistant.Attributes;
using OSISDiscordAssistant.Models;
using OSISDiscordAssistant.Utilities;
using OSISDiscordAssistant.Enums;
using OSISDiscordAssistant.Entities;

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

                    Events eventData = FetchEventData(eventNameResult.Result.Content, EventSearchMode.Exact);

                    if (eventData.EventName is not null)
                    {
                        await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} The event {Formatter.InlineCode(eventNameResult.Result.Content)} already exists! Try again with a different name.");

                        return;
                    }

                    eventName = eventNameResult.Result.Content;

                    if (eventName.Length > 50)
                    {
                        await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} Maximum character limit of 50 characters exceeded. You must re-run the command to finish.");
                        return;
                    }

                    await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, tag the Ketua / Wakil Ketua Acara for {Formatter.Bold(eventNameResult.Result.Content)}. You have one minute.");
                    var personInChargeResult = await interactivityModule.WaitForMessageAsync
                        (x => x.Author.Id == ctx.User.Id && x.Channel.Id == ctx.Channel.Id, TimeSpan.FromMinutes(1));

                    if (!personInChargeResult.TimedOut)
                    {
                        personInCharge = personInChargeResult.Result.Content;
                        if (eventName.Length > 100)
                        {
                            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} Maximum character limit of 100 characters exceeded. You must re-run the command to finish.");
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
                                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} Maximum character limit of 50 characters exceeded. You must re-run the command to finish.");
                                return;
                            }

                            VerifiedEventDateEntity verifiedEventDateEntity = new VerifiedEventDateEntity();

                            verifiedEventDateEntity = verifiedEventDateEntity.Verify(eventDateResult.Result.Content);

                            if (!verifiedEventDateEntity.Passed)
                            {
                                await ctx.Channel.SendMessageAsync(verifiedEventDateEntity.ErrorMessage);

                                return;
                            }

                            else
                            {
                                eventDateCultureInfo = verifiedEventDateEntity.DateCultureInfo;
                            }

                            await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, enter your event description for {Formatter.Bold(eventNameResult.Result.Content)}. You have one minute.");
                            var eventDescriptionResult = await interactivityModule.WaitForMessageAsync
                                (x => x.Author.Id == ctx.User.Id && x.Channel.Id == ctx.Channel.Id, TimeSpan.FromMinutes(1));

                            if (!eventDescriptionResult.TimedOut)
                            {
                                eventDescription = eventDescriptionResult.Result.Content;
                                if (eventDescription.Length > 255)
                                {
                                    await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} Maximum character limit of 255 characters exceeded. You must re-run the command to finish.");
                                    return;
                                }
                            }

                            else
                            {
                                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[TIMED OUT]")} Event description not entered within given time span. Re-run the command if you still need to create your event.");
                                return;
                            }
                        }

                        else
                        {
                            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[TIMED OUT]")} Event date not entered within given time span. Re-run the command if you still need to create your event.");
                            
                            return;
                        }
                    }

                    else
                    {
                        await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[TIMED OUT]")} Ketua / Wakil Ketua Acara not entered within given time span. Re-run the command if you still need to create your event.");
                        return;
                    }
                }

                else
                {
                    await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[TIMED OUT]")} Event name not entered within given time span. Re-run the command if you still need to create your event.");
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

                    List<Events> eventsData = new List<Events>();

                    foreach (var events in eventsData)
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

                        embedBuilder.AddField($"(ID: {events.Id}) {events.EventName} [{events.EventDate}]", ComposeEventDescriptionField(events), true);

                        eventEmbeds.Add(embedBuilder);
                        counter++;
                    }

                    await notifyMessage.DeleteAsync();

                    if (counter == 0)
                    {
                        await ctx.Channel.SendMessageAsync(embedBuilder.WithDescription($"There are no events registered for the year {Formatter.Underline(DateTime.Now.Year.ToString())}."));
                    }

                    else if (counter == 1)
                    {
                        await ctx.Channel.SendMessageAsync(eventEmbeds.First().WithDescription($"List of all registered events for the year {Formatter.Underline(DateTime.Now.Year.ToString())}. Indexed {counter} ({counter.ToWords()}) events."));
                    }

                    else
                    {
                        foreach (var embed in eventEmbeds)
                        {
                            embed.WithDescription($"List of all registered events for the year {Formatter.Underline(DateTime.Now.Year.ToString())}. Indexed {counter} ({counter.ToWords()}) events. To navigate around the search results, interact with the buttons below.");
                        }

                        var pga = eventEmbeds.Select(x => new Page(string.Empty, x)).ToArray();

                        var interactivity = ctx.Client.GetInteractivity();
                        await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pga, PaginationBehaviour.WrapAround, ButtonPaginationBehavior.Disable);
                    }
                });
            }

            else if (operationSelection == "update")
            {                
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[SYNTAX]")} !event update [EVENT ID or EVENT NAME]\nExample: {Formatter.InlineCode("!event update LDKS 2021")}");
            }

            else if (operationSelection == "get")
            {               
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[SYNTAX]")} !event get [EVENT ID or EVENT NAME]\nExample: {Formatter.InlineCode("!event get LDKS 2021")}");
            }

            else if (operationSelection == "search")
            {            
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[SYNTAX]")} !event search [EVENT NAME]\nExample: {Formatter.InlineCode("!event search LDKS 2021")}");
            }

            else if (operationSelection == "delete")
            {                
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[SYNTAX]")} !event delete [EVENT ID or EVENT NAME]\nExample: {Formatter.InlineCode("!event delete LDKS 2021")}");
            }

            else if (operationSelection == "proposal")
            {                
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[SYNTAX]")} !event proposal [EVENT ID or EVENT NAME]\nExample: {Formatter.InlineCode("!event proposal LDKS 2021")}");
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
        /// <param name="keyword">Row number or event name from the events table to update or delete or search. Optional.</param>
        [RequireMainGuild, RequireAccessRole]
        [Command("event")]
        public async Task Event(CommandContext ctx, string operationSelection, [RemainingText] string keyword)
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

                Events eventData = FetchEventData(keyword, EventSearchMode.ClosestMatching);

                if (eventData is null)
                {
                    await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} An error occured. You must provide the correct ID or name of the event you are updating. Refer to {Formatter.InlineCode("!event list")} or {Formatter.InlineCode("!event search")}.");

                    return;
                }

                // Checks whether the selected event has already expired.
                bool hasExpired = eventData.Expired;

                string previousEventName = eventData.EventName;

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
                            Events getExistingEventName = FetchEventData(eventNameResult.Result.Content, EventSearchMode.Exact);

                            if (getExistingEventName.EventName is not null)
                            {
                                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} The event {Formatter.InlineCode(eventNameResult.Result.Content)} already exists! Try again with a different name.");

                                return;
                            }

                            eventName = eventNameResult.Result.Content;

                            if (eventName.Length > 50)
                            {
                                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} Operation aborted. Maximum character limit of 50 characters exceeded.");
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
                                    rowToUpdate = dbUpdate.Events.SingleOrDefault(x => x.Id == eventData.Id);

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
                            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[TIMED OUT]")} Operation aborted. Event name not entered within given time span.");
                            return;
                        }
                    }

                    else if (selectionResult.Result.Emoji == numberTwoEmoji)
                    {
                        await updateEmbed.DeleteAllReactionsAsync();

                        string previousPersonInCharge = eventData.PersonInCharge;

                        var inputInteractivity = ctx.Client.GetInteractivity();

                        var updatePersonInChargeMessage = await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, enter your new Ketua / Wakil Ketua Acara for {Formatter.Bold(previousEventName)}. You have one minute.");
                        var eventPersonInChargeResult = await inputInteractivity.WaitForMessageAsync
                            (x => x.Author.Id == ctx.User.Id && x.Channel.Id == ctx.Channel.Id, TimeSpan.FromMinutes(1));

                        if (!eventPersonInChargeResult.TimedOut)
                        {
                            personInCharge = eventPersonInChargeResult.Result.Content;

                            if (eventName.Length > 100)
                            {
                                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} Operation aborted. Maximum character limit of 100 characters exceeded.");
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
                                    rowToUpdate = dbUpdate.Events.SingleOrDefault(x => x.Id == eventData.Id);

                                    if (rowToUpdate != null)
                                    {
                                        rowToUpdate.PersonInCharge = personInCharge;
                                    }

                                    dbUpdate.SaveChanges();

                                    await dbUpdate.DisposeAsync();
                                }

                                embedBuilder.Title = $"Events Manager - {previousEventName} Update Details";
                                embedBuilder.Description = $"{ctx.Member.Mention} has made update(s) to {previousEventName}.\n\n{Formatter.Bold("Changes made:")}\n• Changed Ketua / Wakil Ketua Acara from {Formatter.InlineCode(previousPersonInCharge)} to {Formatter.InlineCode(personInCharge)}.";
                                embedBuilder.Timestamp = DateTime.Now;

                                await ctx.Channel.SendMessageAsync(embed: embedBuilder);
                            });
                        }

                        else
                        {
                            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[TIMED OUT]")} Operation aborted. Event name not entered within given time span.");
                            return;
                        }
                    }

                    else if (selectionResult.Result.Emoji == numberThreeEmoji)
                    {
                        await updateEmbed.DeleteAllReactionsAsync();

                        string previousEventDate = eventData.EventDate;

                        var inputInteractivity = ctx.Client.GetInteractivity();

                        var updateDateMessage = await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, enter your new event date for {Formatter.Bold(previousEventName)}. You have one minute.");
                        var eventDateResult = await inputInteractivity.WaitForMessageAsync
                            (x => x.Author.Id == ctx.User.Id && x.Channel.Id == ctx.Channel.Id, TimeSpan.FromMinutes(1));

                        if (!eventDateResult.TimedOut)
                        {
                            eventDate = eventDateResult.Result.Content;
                            if (eventDate.Length > 50)
                            {
                                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} Operation aborted. Maximum character limit of 50 characters exceeded.");
                                return;
                            }

                            VerifiedEventDateEntity verifiedEventDateEntity = new VerifiedEventDateEntity();

                            verifiedEventDateEntity = verifiedEventDateEntity.Verify(eventDateResult.Result.Content);

                            if (!verifiedEventDateEntity.Passed)
                            {
                                await ctx.Channel.SendMessageAsync(verifiedEventDateEntity.ErrorMessage);

                                return;
                            }

                            else
                            {
                                eventDateCultureInfo = verifiedEventDateEntity.DateCultureInfo;
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
                                    rowToUpdate = dbUpdate.Events.SingleOrDefault(x => x.Id == eventData.Id);

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
                            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[TIMED OUT]")} Operation aborted. Event date not entered within given time span.");
                            return;
                        }
                    }

                    else if (selectionResult.Result.Emoji == numberFourEmoji)
                    {
                        await updateEmbed.DeleteAllReactionsAsync();

                        string previousEventDescription = eventData.EventDescription;

                        var inputInteractivity = ctx.Client.GetInteractivity();

                        var updateDescriptionMessage = await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, enter your new event description for {Formatter.Bold(previousEventName)}. You have one minute.");
                        var eventDescriptionResult = await inputInteractivity.WaitForMessageAsync
                            (x => x.Author.Id == ctx.User.Id && x.Channel.Id == ctx.Channel.Id, TimeSpan.FromMinutes(1));

                        if (!eventDescriptionResult.TimedOut)
                        {
                            eventDescription = eventDescriptionResult.Result.Content;
                            if (eventDescription.Length > 254)
                            {
                                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} Operation aborted. Maximum character limit of 255 characters exceeded.");
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
                                    rowToUpdate = dbUpdate.Events.SingleOrDefault(x => x.Id == eventData.Id);

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
                            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[TIMED OUT]")} Operation aborted. Event date not entered within given time span.");
                            return;
                        }
                    }
                }

                else
                {
                    await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[TIMED OUT]")} Update selection for {previousEventName} not selected within given time span. Re-run the command if you still need to update your event.");
                }
            }

            else if (operationSelection == "delete")
            {
                Events eventData = FetchEventData(keyword, EventSearchMode.Exact);

                if (eventData is null)
                {
                    await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} An error occured. You must provide the exact ID or name of the event you are looking for. Refer to {Formatter.InlineCode("!event list")} or {Formatter.InlineCode("!event search")}.");

                    return;
                }

                // Checks whether the selected event is already expired
                if (eventData.Expired)
                {
                    await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} Oops! You can only delete events that has not yet expired.");

                    return;
                }

                using (var db = new EventContext())
                {
                    db.Remove(eventData);
                    db.SaveChanges();

                    await ctx.Channel.SendMessageAsync($"{Formatter.Bold(eventData.EventName)} (Event ID: {eventData.Id.ToString()}) successfully deleted from Events Manager.");
                }
            }

            else if (operationSelection == "search")
            {
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

                int counter = 0;

                List<DiscordEmbedBuilder> eventEmbeds = new List<DiscordEmbedBuilder>();

                List<Events> eventsData = FetchAllEventsData(false, keyword);

                foreach (var events in eventsData)
                {
                    var resultEmbed = new DiscordEmbedBuilder
                    {
                        Title = "Events Manager - Search Results",
                        Timestamp = DateTime.Now,
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = "OSIS Discord Assistant"
                        },
                        Color = DiscordColor.MidnightBlue
                    };

                    DateTime eventDate = DateTime.Parse(events.EventDate, new CultureInfo(events.EventDateCultureInfo));

                    resultEmbed.AddField($"(ID: {events.Id}) {events.EventName} [{events.EventDate}]", ComposeEventDescriptionField(events), true);

                    eventEmbeds.Add(resultEmbed);
                    counter++;
                }

                if (counter == 0)
                {
                    await ctx.Channel.SendMessageAsync(embedBuilder.WithDescription($"Oops! There are no results for keyword {Formatter.InlineCode(keyword)}! If you are getting an event by ID, use {Formatter.InlineCode("!event get")}."));
                }

                else if (counter == 1)
                {
                    await ctx.Channel.SendMessageAsync(eventEmbeds.First().WithDescription($"Showing {counter} ({counter.ToWords()}) search result for keyword {Formatter.InlineCode(keyword)}."));
                }

                else
                {
                    foreach (var embed in eventEmbeds)
                    {
                        embed.WithDescription($"Showing {counter} ({counter.ToWords()}) search results for keyword {Formatter.InlineCode(keyword)}. To navigate around the search results, interact with the buttons below.");
                    }

                    var pga = eventEmbeds.Select(x => new Page(string.Empty, x)).ToArray();

                    var interactivity = ctx.Client.GetInteractivity();
                    await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pga, PaginationBehaviour.WrapAround, ButtonPaginationBehavior.Disable);
                }
            }

            else if (operationSelection == "get")
            {
                bool isNumber = int.TryParse(keyword, out int rowIDRaw);

                Events eventData = FetchEventData(keyword, EventSearchMode.Exact);

                if (eventData is null)
                {
                    await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} An error occured. You must provide the exact ID or name of the event you are looking for. Refer to {Formatter.InlineCode("!event list")} or {Formatter.InlineCode("!event search")}.");

                    return;
                }

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

                DateTime eventDate = DateTime.Parse(eventData.EventDate, new CultureInfo(eventData.EventDateCultureInfo));

                embedBuilder.AddField($"(ID: {eventData.Id}) {eventData.EventName} [{eventData.EventDate}]", ComposeEventDescriptionField(eventData), true);

                if (isNumber)
                {
                    embedBuilder.Description = $"Showing result for event ID {rowIDRaw.ToString()}...";
                }

                else
                {
                    embedBuilder.Description = $"Showing result for event name {keyword}...";
                }

                await ctx.Channel.SendMessageAsync(embed: embedBuilder);
            }

            else if (operationSelection == "proposal")
            {
                Events eventData = FetchEventData(keyword, EventSearchMode.ClosestMatching);

                if (eventData is null)
                {
                    await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} An error occured. You must provide the correct ID or name of the event you are looking for. Refer to {Formatter.InlineCode("!event list")} or {Formatter.InlineCode("!event search")}.");

                    return;
                }

                string eventName = eventData.EventName;

                int rowID = eventData.Id;

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

                bool proposalExist = eventData.ProposalFileTitle is null;

                switch (proposalExist)
                {
                    case true:
                        fileExist = "does not";
                        break;
                    case false:
                        fileExist = "does";
                        break;
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
                        if (eventData.ProposalFileContent is null)
                        {
                            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} Event {Formatter.Bold(eventName)} does not have a proposal file stored!");

                            return;
                        }

                        fileTitle = eventData.ProposalFileTitle;

                        fileContent = eventData.ProposalFileContent;

                        fileStream = new MemoryStream(fileContent);

                        messageBuilder.WithFiles(new Dictionary<string, Stream>() { { fileTitle, fileStream } }, true);

                        await ctx.Channel.SendMessageAsync(builder: messageBuilder);
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
                    var eventEmbeds = new List<DiscordEmbedBuilder>();

                    int counter = 0;

                    List<Events> eventsData = FetchAllEventsData(true, keyword);

                    foreach (var events in eventsData)
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

                        embedBuilder.AddField($"(ID: {events.Id}) {events.EventName} [{events.EventDate}]", ComposeEventDescriptionField(events), true);

                        eventEmbeds.Add(embedBuilder);
                        counter++;
                    }

                    await notifyMessage.DeleteAsync();

                    if (counter == 0)
                    {
                        await ctx.Channel.SendMessageAsync(embed: embedBuilder.WithDescription($"There are no events registered for the year {Formatter.Underline(keyword)}."));
                    }

                    else if (counter == 1)
                    {
                        await ctx.Channel.SendMessageAsync(eventEmbeds.First().WithDescription($"List of all registered events for the year {Formatter.Underline(keyword)}. Indexed {counter} ({counter.ToWords()}) events."));
                    }

                    else
                    {
                        foreach (var embed in eventEmbeds)
                        {
                            embed.WithDescription($"List of all registered events for the year {Formatter.Underline(keyword)}. Indexed {counter} ({counter.ToWords()}) events. To navigate around the search results, interact with the buttons below.");
                        }

                        var pga = eventEmbeds.Select(x => new Page(string.Empty, x)).ToArray();

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
        /// Fetches the event data by the given name or ID. This function returns a single <see cref="Events" /> object once it matches with the given criteria.
        /// Not to be confused with indexing a list of events (<see cref="FetchAllEventsData(bool, string)" />) where this function returns more than one <see cref="Events" /> object.
        /// </summary>
        /// <param name="eventNameOrId">The name of the event or the row ID.</param>
        /// <param name="searchMode">The search strategy that tells how to search the event, when fetching via name.</param>
        /// <returns>The <see cref="Events" /> object.</returns>
        internal Events FetchEventData(string keyword, EventSearchMode searchMode)
        {
            using (var db = new EventContext())
            {
                bool isNumber = int.TryParse(keyword, out int rowIDRaw);

                if (isNumber)
                {
                    return db.Events.SingleOrDefault(x => x.Id == rowIDRaw);
                }

                else
                {
                    if (searchMode is EventSearchMode.Exact)
                    {
                        return db.Events.SingleOrDefault(x => x.EventName == keyword.ToLowerInvariant());
                    }

                    else if (searchMode is EventSearchMode.ClosestMatching)
                    {
                        return db.Events.SingleOrDefault(x => x.EventName.Contains(keyword.ToLowerInvariant()));
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Fetches a list of events data by the given keyword. This function returns a list of <see cref="Events" /> object.
        /// </summary>
        /// <param name="indexYear">Sets whether to search events based on the year or process them as a whole.</param>
        /// <param name="keyword">The keyword of the search.</param>
        /// <returns>A <see cref="List{T}" /> of <see cref="Events" /> object.</returns>
        internal List<Events> FetchAllEventsData(bool indexYear, string keyword)
        {
            List<Events> eventsData = new List<Events>();

            using (var db = new EventContext())
            {
                if (indexYear)
                {
                    bool conversionSuccessful = int.TryParse(keyword, out int year);

                    if (!conversionSuccessful)
                    {
                        throw new Exception($"I can only accept years, not dates! Example: !event list 2019");
                    }

                    foreach (var events in db.Events)
                    {
                        DateTime eventDate = DateTime.Parse(events.EventDate, new CultureInfo(events.EventDateCultureInfo));

                        if (year == eventDate.Year)
                        {
                            eventsData.Add(events);
                        }
                    }
                }

                else
                {
                    foreach (var events in db.Events)
                    {
                        if (events.EventName.ToLowerInvariant().Contains(keyword.ToLowerInvariant()))
                        {
                            eventsData.Add(events);
                        }
                    }
                }
            }

            return eventsData;
        }

        /// <summary>
        /// Composes the embed field's value for the respective event.
        /// </summary>
        /// <returns>A string containing the details of the respective event.</returns>
        internal string ComposeEventDescriptionField(Events events)
        {
            DateTime eventDate = DateTime.Parse(events.EventDate, new CultureInfo(events.EventDateCultureInfo));

            bool isProposalEmpty = events.ProposalFileTitle is null ? false : true;

            if (DateTime.Now < eventDate)
            {
                TimeSpan remainingDateTime = eventDate - DateTime.Now;

                return $"Status: {ClientUtilities.ConvertBoolValue(events.Expired, ConvertBoolOption.UpcomingOrDone)} ({Formatter.Timestamp(remainingDateTime, TimestampFormat.RelativeTime)})\nKetua / Wakil Ketua Acara: {events.PersonInCharge}\nProposal: {ClientUtilities.ConvertBoolValue(isProposalEmpty, ConvertBoolOption.StoredOrNotStored)}\nDescription: {events.EventDescription}";
            }

            else
            {
                return $"Status: {ClientUtilities.ConvertBoolValue(events.Expired, ConvertBoolOption.UpcomingOrDone)}\nKetua / Wakil Ketua Acara: {events.PersonInCharge}\nProposal: {ClientUtilities.ConvertBoolValue(isProposalEmpty, ConvertBoolOption.StoredOrNotStored)}\nDescription: {events.EventDescription}";
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

            var errorMessage = await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} The option {Formatter.InlineCode(operationSelection)} does not exist! Type {Formatter.InlineCode("!event")} to list all options. Alternatively, click the emoji below to get help.");

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
