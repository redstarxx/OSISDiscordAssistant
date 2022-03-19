using System;
using System.Linq;
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
using Microsoft.EntityFrameworkCore;

namespace OSISDiscordAssistant.Commands
{
    class EventCommandsModule : BaseCommandModule
    {
        private readonly EventContext _eventContext;

        public EventCommandsModule(EventContext eventContext)
        {
            _eventContext = eventContext;
        }

        #region ARTEMIS (Automated Reminder & Event Management System)
        /// <summary>
        /// Command to view the Events Manager commands and help.
        /// </summary>
        [RequireMainGuild, RequireAccessRole]
        [Command("event")]
        public async Task EventCreateOrList(CommandContext ctx)
        {
            var mainMessageBuilder = new DiscordMessageBuilder();

            var mainEmbed = new DiscordEmbedBuilder
            {
                Title = "ARTEMIS (Automated Reminder & Event Management System) - Overview",
                Timestamp = DateTime.Now,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "OSIS Discord Assistant"
                },
                Color = DiscordColor.MidnightBlue
            };

            mainEmbed.Description = "ARTEMIS integrates event management, proposal submission reminder, and days left to event date reminder under one bot.\n\n" +
                $"{Formatter.Bold("CREATE")} - Creates a new event.\n" +
                $"{Formatter.Bold("UPDATE")} - Updates an event.\n" +
                $"{Formatter.Bold("SEARCH")} - Search for an event which name contains the given keyword.\n" +
                $"{Formatter.Bold("GET")} - Gets an event directly with the provided name (must be exact) or ID.\n" +
                $"{Formatter.Bold("LIST")} - Lists all registered events for the desired year.\n" +
                $"{Formatter.Bold("PROPOSAL")} - Gets, updates, or deletes the proposal file for the respective event.\n\n" +
                $"{Formatter.Bold("DELETE")} - Deletes an event.\n" +
                $"You have 5 (five) minutes to click one of the buttons below.";

            var firstRowButtons = new DiscordButtonComponent[]
            {
                new DiscordButtonComponent(ButtonStyle.Success, $"create_event_{ClientUtilities.GetCurrentUnixTimestamp()}", "CREATE", false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":pencil:"))),
                new DiscordButtonComponent(ButtonStyle.Secondary, $"update_event_{ClientUtilities.GetCurrentUnixTimestamp()}", "UPDATE", false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":wrench:"))),
                new DiscordButtonComponent(ButtonStyle.Secondary, $"search_event_{ClientUtilities.GetCurrentUnixTimestamp()}", "SEARCH", false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":mag:"))),
                new DiscordButtonComponent(ButtonStyle.Secondary, $"get_event_{ClientUtilities.GetCurrentUnixTimestamp()}", "GET", false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":one:"))),
                new DiscordButtonComponent(ButtonStyle.Secondary, $"list_event_{ClientUtilities.GetCurrentUnixTimestamp()}", "LIST", false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":clipboard:"))),
            };

            var secondRowButtons = new DiscordButtonComponent[]
            {
                new DiscordButtonComponent(ButtonStyle.Primary, $"proposal_event_{ClientUtilities.GetCurrentUnixTimestamp()}", "PROPOSAL", false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":card_box:"))),
                new DiscordButtonComponent(ButtonStyle.Danger, $"delete_event_{ClientUtilities.GetCurrentUnixTimestamp()}", "DELETE", false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":put_litter_in_its_place:")))
            };

            mainMessageBuilder.WithEmbed(mainEmbed).AddComponents(firstRowButtons).AddComponents(secondRowButtons);

            var message = await ctx.Channel.SendMessageAsync(mainMessageBuilder);

            var interactivity = ctx.Client.GetInteractivity();

            var mainButtonResult = await interactivity.WaitForButtonAsync(message, ctx.User, TimeSpan.FromMinutes(5));

            if (!mainButtonResult.TimedOut)
            {
                foreach (var component in firstRowButtons)
                {
                    component.Disable();
                }

                foreach (var component in secondRowButtons)
                {
                    component.Disable();
                }

                await mainButtonResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder()
                    .AddEmbed(mainEmbed)
                    .AddComponents(firstRowButtons)
                    .AddComponents(secondRowButtons));

                if (mainButtonResult.Result.Id.Contains("create_event_"))
                {
                    string eventName = null;
                    string personInCharge = null;
                    long eventDate = 0;
                    string eventDescription = null;

                    await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, enter your event name. You have five minutes.");
                    var interactivityModule = ctx.Client.GetInteractivity();
                    var eventNameResult = await interactivityModule.WaitForMessageAsync
                        (x => x.Author.Id == ctx.User.Id && (x.Channel.Id == ctx.Channel.Id), TimeSpan.FromMinutes(5));

                    if (!eventNameResult.TimedOut)
                    {
                        // Checks whether the given event name matches with another event that has the exact name.
                        Events eventData = FetchEventData(eventNameResult.Result.Content, EventSearchMode.Exact);

                        if (eventData is null)
                        {
                            eventName = eventNameResult.Result.Content;
                        }

                        else
                        {
                            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} There is an event already stored with the same name! Try again with a different name.");

                            return;
                        }

                        if (eventName.Length > 50)
                        {
                            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} Maximum character limit of 50 characters exceeded. You must re-run the command to finish.");

                            return;
                        }

                        await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, tag the Ketua / Wakil Ketua Acara for {Formatter.Bold(eventNameResult.Result.Content)}. You have five minutes.");
                        var personInChargeResult = await interactivityModule.WaitForMessageAsync
                            (x => x.Author.Id == ctx.User.Id && x.Channel.Id == ctx.Channel.Id, TimeSpan.FromMinutes(5));

                        if (!personInChargeResult.TimedOut)
                        {
                            personInCharge = personInChargeResult.Result.Content;
                            if (eventName.Length > 100)
                            {
                                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} Maximum character limit of 100 characters exceeded. You must re-run the command to finish.");
                                return;
                            }

                            await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, enter your event date for {Formatter.Bold(eventNameResult.Result.Content)}. You have five minutes.");
                            var eventDateResult = await interactivityModule.WaitForMessageAsync
                                (x => x.Author.Id == ctx.User.Id && x.Channel.Id == ctx.Channel.Id, TimeSpan.FromMinutes(5));

                            if (!eventDateResult.TimedOut)
                            {
                                if (eventDateResult.Result.Content.Length > 50)
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

                                eventDate = verifiedEventDateEntity.EventDateUnixTimeStamp;

                                await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, enter your event description for {Formatter.Bold(eventNameResult.Result.Content)}. You have five minutes.");
                                var eventDescriptionResult = await interactivityModule.WaitForMessageAsync
                                    (x => x.Author.Id == ctx.User.Id && x.Channel.Id == ctx.Channel.Id, TimeSpan.FromMinutes(5));

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
                                    await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[TIMED OUT]")} Event description not entered within five minutes. Re-run the command if you still need to create your event.");
                                    return;
                                }
                            }

                            else
                            {
                                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[TIMED OUT]")} Event date not entered within five minutes. Re-run the command if you still need to create your event.");

                                return;
                            }
                        }

                        else
                        {
                            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[TIMED OUT]")} Ketua / Wakil Ketua Acara not entered within five minutes. Re-run the command if you still need to create your event.");
                            return;
                        }
                    }

                    else
                    {
                        await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[TIMED OUT]")} Event name not entered within five minutes. Re-run the command if you still need to create your event.");
                        return;
                    }

                    _ = Task.Run(async () =>
                    {
                        Events eventData = ClientUtilities.CalculateEventReminderDate(eventDate);

                        _eventContext.Add(new Events
                        {
                            EventName = eventName,
                            PersonInCharge = personInCharge,
                            EventDateUnixTimestamp = eventDate,
                            NextScheduledReminderUnixTimestamp = eventData.NextScheduledReminderUnixTimestamp,
                            EventDescription = eventDescription,
                            ExecutedReminderLevel = eventData.ExecutedReminderLevel,
                            Expired = eventData.Expired
                        });

                        await _eventContext.SaveChangesAsync();

                        var eventId = _eventContext.Events.FirstOrDefault(x => x.EventName == eventName).Id;

                        _ = eventData.NextScheduledReminderUnixTimestamp is not 0 ? await ctx.Channel.SendMessageAsync($"Okay {ctx.Member.Mention}, {Formatter.Bold(eventName)} (ID: {eventId}) has been created.\nReminders have been set and will be sent at {Formatter.Timestamp(ClientUtilities.ConvertUnixTimestampToDateTime(eventData.NextScheduledReminderUnixTimestamp), TimestampFormat.LongDateTime)}.") : await ctx.Channel.SendMessageAsync($"Okay {ctx.Member.Mention}, {Formatter.Bold(eventName)} (ID: {eventId}) has been created.");
                    });
                }

                else if (mainButtonResult.Result.Id.Contains("update_event_") || mainButtonResult.Result.Id.Contains("search_event_") || mainButtonResult.Result.Id.Contains("get_event_") || mainButtonResult.Result.Id.Contains("delete_event_") || mainButtonResult.Result.Id.Contains("proposal_event_"))
                {
                    string keyword = "";
                    bool isNumber;

                    await ctx.Channel.SendMessageAsync("Please type your desired event name or ID. You have 5 (five) minutes.");

                    var targetEventResult = await interactivity.WaitForMessageAsync
                        (x => x.Author.Id == ctx.User.Id && x.Channel.Id == ctx.Channel.Id, TimeSpan.FromMinutes(5));

                    if (!targetEventResult.TimedOut)
                    {
                        keyword = targetEventResult.Result.Content;

                        isNumber = int.TryParse(keyword, out int id);
                    }

                    else
                    {
                        await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[TIMED OUT]")} {ctx.Member.Mention} You are taking too long to type your event name or ID. Try again.");

                        return;
                    }

                    if (mainButtonResult.Result.Id.Contains("update_event_"))
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
                            _ = isNumber is false ? await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} No such event is found with the keyword {Formatter.InlineCode(keyword)}. You must provide the correct ID or name of the event you are updating. Refer to {Formatter.InlineCode("LIST")} or {Formatter.InlineCode("SEARCH")} button.") : await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} No such event is found with the ID {Formatter.InlineCode(keyword)}. You must provide the correct ID or name of the event you are updating. Refer to {Formatter.InlineCode("LIST")} or {Formatter.InlineCode("SEARCH")} button.");

                            return;
                        }

                        string reminderSettingStatus = eventData.ReminderDisabled is false ? "Disable" : "Enable";

                        embedBuilder.Title = $"Events Manager - Updating {eventData.EventName} (ID: {eventData.Id})...";
                        embedBuilder.Description = $"Choose either one of the following buttons to select what are you going to change from {Formatter.Bold(eventData.EventName)}.\n\n" +
                            $"You have 5 (five) minutes to make your choice.";

                        var buttonOptions = new DiscordButtonComponent[]
                        {
                            new DiscordButtonComponent(ButtonStyle.Secondary, $"event_name_{ClientUtilities.GetCurrentUnixTimestamp()}", "CHANGE EVENT NAME", false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":placard:"))),
                            new DiscordButtonComponent(ButtonStyle.Secondary, $"event_person_in_charge_{ClientUtilities.GetCurrentUnixTimestamp()}", "CHANGE KETUA / WAKIL KETUA EVENT", false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":busts_in_silhouette:"))),
                            new DiscordButtonComponent(ButtonStyle.Secondary, $"event_date_{ClientUtilities.GetCurrentUnixTimestamp()}", "CHANGE EVENT DATE", false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":date:"))),
                            new DiscordButtonComponent(ButtonStyle.Secondary, $"event_description_{ClientUtilities.GetCurrentUnixTimestamp()}", "CHANGE EVENT DESCRIPTION", false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":bookmark:"))),
                            new DiscordButtonComponent(ButtonStyle.Secondary, $"event_reminder_{ClientUtilities.GetCurrentUnixTimestamp()}", $"{reminderSettingStatus.ToUpperInvariant()} REMINDERS", false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":alarm_clock:")))
                        };

                        var messageBuilder = new DiscordMessageBuilder()
                            .WithEmbed(embedBuilder)
                            .AddComponents(buttonOptions);

                        var updateMessage = await ctx.Channel.SendMessageAsync(messageBuilder);

                        var buttonResult = await interactivity.WaitForButtonAsync(updateMessage, ctx.User, TimeSpan.FromMinutes(5));

                        if (!buttonResult.TimedOut)
                        {
                            foreach (var component in buttonOptions)
                            {
                                component.Disable();
                            }

                            await buttonResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, 
                                new DiscordInteractionResponseBuilder()
                                .AddEmbed(embedBuilder).AddComponents(buttonOptions));

                            if (buttonResult.Result.Id.Contains("event_name_"))
                            {
                                await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, enter your new event name for {Formatter.Bold(eventData.EventName)}. You have five minutes.");

                                var eventNameResult = await interactivity.WaitForMessageAsync
                                    (x => x.Author.Id == ctx.User.Id && x.Channel.Id == ctx.Channel.Id, TimeSpan.FromMinutes(5));

                                if (!eventNameResult.TimedOut)
                                {
                                    Events getExistingEventName = FetchEventData(eventNameResult.Result.Content, EventSearchMode.Exact);

                                    if (getExistingEventName is not null)
                                    {
                                        await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} The event {Formatter.InlineCode(eventNameResult.Result.Content)} already exists! Try again with a different name.");

                                        return;
                                    }

                                    if (eventNameResult.Result.Content.Length > 50)
                                    {
                                        await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} Operation aborted. Maximum character limit of 50 characters exceeded.");
                                        return;
                                    }

                                    if (eventNameResult.Result.Content == "abort")
                                    {
                                        await ctx.Channel.SendMessageAsync($"Update operation aborted as per your request, {ctx.Member.Mention}.");
                                        return;
                                    }

                                    _ = Task.Run(async () =>
                                    {
                                        Events rowToUpdate = _eventContext.Events.SingleOrDefault(x => x.Id == eventData.Id);

                                        if (rowToUpdate != null)
                                        {
                                            rowToUpdate.EventName = eventNameResult.Result.Content;
                                        }

                                        await _eventContext.SaveChangesAsync();

                                        embedBuilder.Title = $"Events Manager - {eventData.EventName} (ID: {eventData.Id}) Update Details";
                                        embedBuilder.Description = $"{ctx.Member.Mention} has made update(s) to {eventData.EventName}.\n\n{Formatter.Bold("Changes made:")}\n• Changed event name from {Formatter.InlineCode(eventData.EventName)} to {Formatter.InlineCode(eventNameResult.Result.Content)}.";
                                        embedBuilder.Timestamp = DateTime.Now;

                                        await ctx.Channel.SendMessageAsync(embed: embedBuilder);
                                    });
                                }

                                else
                                {
                                    await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[TIMED OUT]")} Operation aborted. Event name not entered within five minutes.");
                                    return;
                                }
                            }

                            else if (buttonResult.Result.Id.Contains("event_person_in_charge"))
                            {
                                await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, enter your new Ketua / Wakil Ketua Acara for {Formatter.Bold(eventData.EventName)}. You have five minutes.");

                                var eventPersonInChargeResult = await interactivity.WaitForMessageAsync
                                    (x => x.Author.Id == ctx.User.Id && x.Channel.Id == ctx.Channel.Id, TimeSpan.FromMinutes(5));

                                if (!eventPersonInChargeResult.TimedOut)
                                {
                                    if (eventPersonInChargeResult.Result.Content.Length > 100)
                                    {
                                        await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} Operation aborted. Maximum character limit of 100 characters exceeded.");
                                        return;
                                    }

                                    if (eventPersonInChargeResult.Result.Content == "abort")
                                    {
                                        await ctx.Channel.SendMessageAsync($"Update operation aborted as per your request, {ctx.Member.Mention}.");
                                        return;
                                    }

                                    _ = Task.Run(async () =>
                                    {
                                        Events rowToUpdate = _eventContext.Events.SingleOrDefault(x => x.Id == eventData.Id);

                                        if (rowToUpdate != null)
                                        {
                                            rowToUpdate.PersonInCharge = eventPersonInChargeResult.Result.Content;
                                        }

                                        await _eventContext.SaveChangesAsync();

                                        embedBuilder.Title = $"Events Manager - {eventData.EventName} (ID: {eventData.Id}) Update Details";
                                        embedBuilder.Description = $"{ctx.Member.Mention} has made update(s) to {eventData.EventName}.\n\n{Formatter.Bold("Changes made:")}\n• Changed Ketua / Wakil Ketua Acara from {Formatter.InlineCode(eventData.PersonInCharge)} to {Formatter.InlineCode(eventPersonInChargeResult.Result.Content)}.";
                                        embedBuilder.Timestamp = DateTime.Now;

                                        await ctx.Channel.SendMessageAsync(embed: embedBuilder);
                                    });
                                }

                                else
                                {
                                    await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[TIMED OUT]")} Operation aborted. Event name not entered within five minutes.");
                                    return;
                                }
                            }

                            else if (buttonResult.Result.Id.Contains("event_date_"))
                            {
                                await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, enter your new event date for {Formatter.Bold(eventData.EventName)}. You have five minutes.");

                                var eventDateResult = await interactivity.WaitForMessageAsync
                                    (x => x.Author.Id == ctx.User.Id && x.Channel.Id == ctx.Channel.Id, TimeSpan.FromMinutes(5));

                                if (!eventDateResult.TimedOut)
                                {
                                    if (eventDateResult.Result.Content.Length > 50)
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

                                    var buttonConfirmationOptions = new DiscordButtonComponent[]
                                    {
                                        new DiscordButtonComponent(ButtonStyle.Success, $"confirm_date_update_{ClientUtilities.GetCurrentUnixTimestamp()}", "YES", false, null),
                                        new DiscordButtonComponent(ButtonStyle.Danger, $"cancel_date_update_{ClientUtilities.GetCurrentUnixTimestamp()}", "CANCEL", false, null)
                                    };

                                    var confirmationMessageBuilder = new DiscordMessageBuilder()
                                        .WithContent($"{Formatter.Bold("[WARNING]")} By updating the date of {Formatter.Bold(eventData.EventName)}, it will reset the reminder for this event and all members may be pinged again to remind them if a reminder for this event was sent previously. Proceed?")
                                        .AddComponents(buttonConfirmationOptions);

                                    var confirmationMessage = await ctx.Channel.SendMessageAsync(confirmationMessageBuilder);

                                    var warningInteractivityResult = await interactivity.WaitForButtonAsync(confirmationMessage, ctx.User, TimeSpan.FromMinutes(15));

                                    if (!warningInteractivityResult.TimedOut)
                                    {
                                        foreach (var component in buttonConfirmationOptions)
                                        {
                                            component.Disable();
                                        }

                                        await warningInteractivityResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, 
                                            new DiscordInteractionResponseBuilder()
                                            .WithContent(confirmationMessageBuilder.Content)
                                            .AddComponents(buttonConfirmationOptions));

                                        if (warningInteractivityResult.Result.Id.Contains("confirm_date_update_"))
                                        {
                                            await ctx.Channel.SendMessageAsync("Okay.");
                                        }

                                        else if (warningInteractivityResult.Result.Id.Contains("cancel_date_update_"))
                                        {
                                            await ctx.Channel.SendMessageAsync($"Cancellation acknowledged. Aborted updating {Formatter.Bold(eventData.EventName)}.");

                                            return;
                                        }
                                    }

                                    else
                                    {
                                        await ctx.RespondAsync($"{Formatter.Bold("[TIMED OUT]")} {ctx.Member.Mention} You're taking too long to decide. Feel free to try updating {Formatter.Bold(eventData.EventName)} again.");

                                        return;
                                    }

                                    _ = Task.Run(async () =>
                                    {
                                        Events rowToUpdate = _eventContext.Events.SingleOrDefault(x => x.Id == eventData.Id);

                                        if (rowToUpdate != null)
                                        {
                                            Events calculatedEventReminderData = ClientUtilities.CalculateEventReminderDate(verifiedEventDateEntity.EventDateUnixTimeStamp);

                                            rowToUpdate.EventDateUnixTimestamp = calculatedEventReminderData.EventDateUnixTimestamp;
                                            rowToUpdate.NextScheduledReminderUnixTimestamp = calculatedEventReminderData.NextScheduledReminderUnixTimestamp;
                                            rowToUpdate.Expired = calculatedEventReminderData.Expired;
                                            rowToUpdate.ExecutedReminderLevel = calculatedEventReminderData.ExecutedReminderLevel;
                                            rowToUpdate.ProposalReminded = false;

                                            await _eventContext.SaveChangesAsync();
                                        }

                                        embedBuilder.Title = $"Events Manager - {eventData.EventName} (ID: {eventData.Id}) Update Details";
                                        embedBuilder.Description = $"{ctx.Member.Mention} has made update(s) to {eventData.EventName}.\n\n{Formatter.Bold("Changes made:")}\n• Changed event date from {Formatter.Timestamp(ClientUtilities.ConvertUnixTimestampToDateTime(eventData.EventDateUnixTimestamp), TimestampFormat.LongDate)} to {Formatter.Timestamp(ClientUtilities.ConvertUnixTimestampToDateTime(rowToUpdate.EventDateUnixTimestamp), TimestampFormat.LongDate)}.";
                                        embedBuilder.Timestamp = DateTime.Now;

                                        await ctx.Channel.SendMessageAsync(embed: embedBuilder);
                                    });
                                }

                                else
                                {
                                    await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[TIMED OUT]")} Operation aborted. Event date not entered within five minutes.");
                                    return;
                                }
                            }

                            else if (buttonResult.Result.Id.Contains("event_description_"))
                            {
                                await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, enter your new event description for {Formatter.Bold(eventData.EventName)}. You have five minutes.");

                                var eventDescriptionResult = await interactivity.WaitForMessageAsync
                                    (x => x.Author.Id == ctx.User.Id && x.Channel.Id == ctx.Channel.Id, TimeSpan.FromMinutes(5));

                                if (!eventDescriptionResult.TimedOut)
                                {
                                    if (eventDescriptionResult.Result.Content.Length > 254)
                                    {
                                        await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} Operation aborted. Maximum character limit of 255 characters exceeded.");
                                        return;
                                    }

                                    if (eventDescriptionResult.Result.Content == "abort")
                                    {
                                        await ctx.Channel.SendMessageAsync($"Update operation aborted as per your request, {ctx.Member.Mention}.");
                                        return;
                                    }

                                    _ = Task.Run(async () =>
                                    {
                                        Events rowToUpdate = _eventContext.Events.SingleOrDefault(x => x.Id == eventData.Id);

                                        if (rowToUpdate != null)
                                        {
                                            rowToUpdate.EventDescription = eventDescriptionResult.Result.Content;

                                            await _eventContext.SaveChangesAsync();
                                        }

                                        embedBuilder.Title = $"Events Manager - {eventData.EventName} (ID: {eventData.Id}) Update Details";
                                        embedBuilder.Description = $"{ctx.Member.Mention} has made update(s) to {eventData.EventName}.\n\n{Formatter.Bold("Changes made:")}\n• Changed event description from {Formatter.InlineCode(eventData.EventDescription)} to {Formatter.InlineCode(eventDescriptionResult.Result.Content)}.";
                                        embedBuilder.Timestamp = DateTime.Now;

                                        await ctx.Channel.SendMessageAsync(embed: embedBuilder);
                                    });
                                }

                                else
                                {
                                    await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[TIMED OUT]")} Operation aborted. Event date not entered within five minutes.");
                                    return;
                                }
                            }

                            else if (buttonResult.Result.Id.Contains("event_reminder_"))
                            {
                                _ = Task.Run(async () =>
                                {
                                    Events rowToUpdate = _eventContext.Events.SingleOrDefault(x => x.Id == eventData.Id);

                                    if (rowToUpdate != null)
                                    {
                                        rowToUpdate.ReminderDisabled = rowToUpdate.ReminderDisabled is true ? false : true;

                                        await _eventContext.SaveChangesAsync();
                                    }

                                    embedBuilder.Title = $"Events Manager - {eventData.EventName} (ID: {eventData.Id}) Update Details";
                                    embedBuilder.Description = $"{ctx.Member.Mention} has made update(s) to {eventData.EventName}.\n\n{Formatter.Bold("Changes made:")}\n• {reminderSettingStatus}d reminders.";
                                    embedBuilder.Timestamp = DateTime.Now;

                                    await ctx.Channel.SendMessageAsync(embed: embedBuilder);
                                });
                            }
                        }

                        else
                        {
                            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[TIMED OUT]")} Update selection for {eventData.EventName} not selected within five minutes. Re-run the command if you still need to update your event.");
                        }
                    }

                    else if (mainButtonResult.Result.Id.Contains("search_event_"))
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

                        IEnumerable<Events> eventsData = FetchAllEventsData(false, keyword);

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

                            resultEmbed.AddField($"(ID: {events.Id}) {events.EventName} [{Formatter.Timestamp(ClientUtilities.ConvertUnixTimestampToDateTime(events.EventDateUnixTimestamp), TimestampFormat.LongDate)}]", ComposeEventDescriptionField(events), true);

                            eventEmbeds.Add(resultEmbed);
                            counter++;
                        }

                        if (counter == 0)
                        {
                            await ctx.Channel.SendMessageAsync(embedBuilder.WithDescription($"Oops! There are no results for keyword {Formatter.InlineCode(keyword)}! If you are getting an event by ID, choose the {Formatter.InlineCode("GET")} button."));
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

                            await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pga, PaginationBehaviour.WrapAround, ButtonPaginationBehavior.Disable);
                        }
                    }

                    else if (mainButtonResult.Result.Id.Contains("get_event_"))
                    {
                        Events eventData = FetchEventData(keyword, EventSearchMode.Exact);

                        if (eventData is null)
                        {
                            _ = isNumber is false ? await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} No such event is found with the keyword {Formatter.InlineCode(keyword)}. You must provide the correct ID or name of the event you are updating. Refer to {Formatter.InlineCode("LIST")} or {Formatter.InlineCode("SEARCH")} button.") : await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} No such event is found with the ID {Formatter.InlineCode(keyword)}. You must provide the correct ID or name of the event you are updating. Refer to {Formatter.InlineCode("LIST")} or {Formatter.InlineCode("SEARCH")} button.");

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

                        embedBuilder.AddField($"(ID: {eventData.Id}) {eventData.EventName} [{Formatter.Timestamp(ClientUtilities.ConvertUnixTimestampToDateTime(eventData.EventDateUnixTimestamp), TimestampFormat.LongDate)}]", ComposeEventDescriptionField(eventData), true);

                        if (isNumber)
                        {
                            embedBuilder.Description = $"Showing result for event ID {keyword}...";
                        }

                        else
                        {
                            embedBuilder.Description = $"Showing result for event name {keyword}...";
                        }

                        await ctx.Channel.SendMessageAsync(embed: embedBuilder);
                    }

                    else if (mainButtonResult.Result.Id.Contains("delete_event_"))
                    {
                        Events eventData = FetchEventData(keyword, EventSearchMode.Exact);

                        if (eventData is null)
                        {
                            _ = isNumber is false ? await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} No such event is found with the keyword {Formatter.InlineCode(keyword)}. You must provide the correct ID or name of the event you are updating. Refer to {Formatter.InlineCode("LIST")} or {Formatter.InlineCode("SEARCH")} button.") : await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} No such event is found with the ID {Formatter.InlineCode(keyword)}. You must provide the correct ID or name of the event you are updating. Refer to {Formatter.InlineCode("LIST")} or {Formatter.InlineCode("SEARCH")} button.");

                            return;
                        }

                        // Checks whether the selected event is already expired
                        if (eventData.Expired)
                        {
                            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} Oops! You can only delete events that has not yet expired.");

                            return;
                        }

                        _eventContext.Remove(eventData);
                        await _eventContext.SaveChangesAsync();

                        await ctx.Channel.SendMessageAsync($"Okay {ctx.Member.Mention}, {Formatter.Bold(eventData.EventName)} (ID: {eventData.Id}) has been deleted from Events Manager.");
                    }

                    else if (mainButtonResult.Result.Id.Contains("proposal_event_"))
                    {
                        Events eventData = FetchEventData(keyword, EventSearchMode.ClosestMatching);

                        if (eventData is null)
                        {
                            _ = isNumber is false ? await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} No such event is found with the keyword {Formatter.InlineCode(keyword)}. You must provide the correct ID or name of the event you are updating. Refer to {Formatter.InlineCode("LIST")} or {Formatter.InlineCode("SEARCH")} button.") : await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} No such event is found with the ID {Formatter.InlineCode(keyword)}. You must provide the correct ID or name of the event you are updating. Refer to {Formatter.InlineCode("LIST")} or {Formatter.InlineCode("SEARCH")} button.");

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

                        embedBuilder.Title = $"Events Manager - Accessing {eventName}'s (ID: {eventData.Id}) Proposal...";
                        embedBuilder.Description = $"Choose either one of the following buttons to select what are you going to do with {Formatter.Bold(eventName)}. This event {Formatter.Underline(fileExist)} have a proposal file stored.\n\n" +
                            $"You have 5 (five) minutes to select your choice.";

                        var buttonOptions = new DiscordButtonComponent[]
                        {
                            new DiscordButtonComponent(ButtonStyle.Secondary, $"get_proposal_{ClientUtilities.GetCurrentUnixTimestamp()}", "GET THE PROPOSAL", false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":inbox_tray:"))),
                            new DiscordButtonComponent(ButtonStyle.Success, $"update_proposal_{ClientUtilities.GetCurrentUnixTimestamp()}", "UPLOAD / UPDATE PROPOSAL", false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":outbox_tray:"))),
                            new DiscordButtonComponent(ButtonStyle.Danger, $"delete_proposal_{ClientUtilities.GetCurrentUnixTimestamp()}", "DELETE PROPOSAL", false, new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":put_litter_in_its_place:")))
                        };

                        var messageBuilder = new DiscordMessageBuilder()
                            .WithEmbed(embedBuilder)
                            .AddComponents(buttonOptions);

                        var updateMessage = await ctx.Channel.SendMessageAsync(messageBuilder);

                        var buttonResult = await interactivity.WaitForButtonAsync(updateMessage, ctx.User, TimeSpan.FromMinutes(5));

                        if (!buttonResult.TimedOut)
                        {
                            foreach (var component in buttonOptions)
                            {
                                component.Disable();
                            }

                            await buttonResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, 
                                new DiscordInteractionResponseBuilder()
                                .AddEmbed(embedBuilder)
                                .AddComponents(buttonOptions));

                            string fileTitle = null;

                            byte[] fileContent = null;

                            MemoryStream fileStream = new MemoryStream();

                            if (buttonResult.Result.Id.Contains("get_proposal"))
                            {
                                if (eventData.ProposalFileContent is null)
                                {
                                    await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} {Formatter.Bold(eventName)} does not have a proposal file stored!");

                                    return;
                                }

                                fileTitle = eventData.ProposalFileTitle;

                                fileContent = eventData.ProposalFileContent;

                                fileStream = new MemoryStream(fileContent);

                                var proposalResponseMessageBuilder = new DiscordMessageBuilder()
                                {
                                    Content = $"{Formatter.Bold(eventName)}'s proposal file is as follows:"
                                };

                                proposalResponseMessageBuilder.WithFiles(new Dictionary<string, Stream>() { { fileTitle, fileStream } }, true);

                                await ctx.Channel.SendMessageAsync(builder: proposalResponseMessageBuilder);
                            }

                            else if (buttonResult.Result.Id.Contains("update_proposal"))
                            {
                                await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, drop / upload the proposal file here. An acceptable file is a Microsoft Word document. You have five minutes!");

                                var proposalResult = await interactivity.WaitForMessageAsync(x => x.Author.Id == ctx.Member.Id, TimeSpan.FromMinutes(5));

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

                                    Events rowToAccess = _eventContext.Events.SingleOrDefault(x => x.Id == rowID);

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

                                    await _eventContext.SaveChangesAsync();

                                    await ctx.Channel.SendMessageAsync($"Okay {ctx.Member.Mention}, {Formatter.Bold(eventName)}'s proposal file has been {fileStatus}!");
                                }

                                else
                                {
                                    await ctx.RespondAsync($"{Formatter.Bold("[TIMED OUT]")} {ctx.Member.Mention} Proposal file is not uploaded within five minutes. Re-run the command if you still need to update {Formatter.Bold(eventName)}'s proposal.");
                                }
                            }

                            else if (buttonResult.Result.Id.Contains("delete_proposal"))
                            {
                                if (eventData.ProposalFileTitle is null)
                                {
                                    await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} {Formatter.Bold(eventName)} does not have a proposal file stored!");

                                    return;
                                }

                                var buttonConfirmationOptions = new DiscordButtonComponent[]
                                {
                                    new DiscordButtonComponent(ButtonStyle.Success, $"confirm_proposal_deletion_{ClientUtilities.GetCurrentUnixTimestamp()}", "YES", false, null),
                                    new DiscordButtonComponent(ButtonStyle.Danger, $"cancel_proposal_deletion_{ClientUtilities.GetCurrentUnixTimestamp()}", "CANCEL", false, null)
                                };

                                var confirmationMessageBuilder = new DiscordMessageBuilder()
                                    .WithContent($"{Formatter.Bold("[WARNING]")} By clicking YES, {Formatter.Bold(eventData.EventName)}'s proposal file will be permanently deleted. There is no guarantee that it could be recovered if this is accidental. Are you sure?")
                                    .AddComponents(buttonConfirmationOptions);

                                var confirmationMessage = await ctx.Channel.SendMessageAsync(confirmationMessageBuilder);

                                var warningInteractivityResult = await interactivity.WaitForButtonAsync(confirmationMessage, ctx.User, TimeSpan.FromMinutes(15));

                                if (!warningInteractivityResult.TimedOut)
                                {
                                    foreach (var component in buttonConfirmationOptions)
                                    {
                                        component.Disable();
                                    }

                                    await warningInteractivityResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, 
                                        new DiscordInteractionResponseBuilder()
                                        .WithContent(confirmationMessageBuilder.Content)
                                        .AddComponents(buttonConfirmationOptions));

                                    if (warningInteractivityResult.Result.Id.Contains("confirm_proposal_deletion_"))
                                    {
                                        Events rowToDelete = _eventContext.Events.SingleOrDefault(x => x.Id == rowID);

                                        rowToDelete.ProposalFileTitle = null;

                                        rowToDelete.ProposalFileContent = null;

                                        await _eventContext.SaveChangesAsync();

                                        await ctx.Channel.SendMessageAsync($"Okay {ctx.Member.Mention}, {Formatter.Bold(eventName)}'s proposal file has been deleted!");
                                    }

                                    else if (warningInteractivityResult.Result.Id.Contains("cancel_proposal_deletion_"))
                                    {
                                        await ctx.Channel.SendMessageAsync($"Cancellation acknowledged. Aborted deleting {Formatter.Bold(eventData.EventName)}'s proposal file.");

                                        return;
                                    }
                                }

                                else
                                {
                                    await ctx.RespondAsync($"{Formatter.Bold("[TIMED OUT]")} {ctx.Member.Mention} You're taking too long to decide. Feel free to try updating {Formatter.Bold(eventData.EventName)} again.");

                                    return;
                                }
                            }

                            await fileStream.DisposeAsync();
                        }

                        else
                        {
                            await ctx.RespondAsync($"{Formatter.Bold("[TIMED OUT]")} {ctx.Member.Mention} You did not choose an option within five minutes. Re-run the command if you still need to update {Formatter.Bold(eventName)}'s proposal.");
                        }
                    }
                }

                else if (mainButtonResult.Result.Id.Contains("list_event_"))
                {
                    string year = "";

                    await ctx.Channel.SendMessageAsync($"Please enter the desired year to list all events in the given year (optionally, type {Formatter.InlineCode("this")} if you want to retrieve this year's event list).");

                    var yearResult = await interactivity.WaitForMessageAsync
                        (x => x.Author.Id == ctx.User.Id && x.Channel.Id == ctx.Channel.Id, TimeSpan.FromMinutes(5));

                    if (!yearResult.TimedOut)
                    {
                        year = yearResult.Result.Content is "this" ? DateTime.Now.Year.ToString() : yearResult.Result.Content;
                    }

                    else
                    {
                        await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[TIMED OUT]")} {ctx.Member.Mention} You are taking too long to type your event name or ID. Try again.");

                        return;
                    }

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

                    _ = Task.Run(async () =>
                    {
                        int counter = 0;

                        List<DiscordEmbedBuilder> eventEmbeds = new List<DiscordEmbedBuilder>();

                        IEnumerable<Events> eventsData = new List<Events>();

                        try
                        {
                            eventsData = FetchAllEventsData(true, year);
                        }

                        catch (Exception ex)
                        {
                            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} {ex.Message}");

                            return;
                        }

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

                            embedBuilder.AddField($"(ID: {events.Id}) {events.EventName} [{Formatter.Timestamp(ClientUtilities.ConvertUnixTimestampToDateTime(events.EventDateUnixTimestamp), TimestampFormat.LongDate)}]", ComposeEventDescriptionField(events), true);

                            eventEmbeds.Add(embedBuilder);
                            counter++;
                        }

                        await notifyMessage.DeleteAsync();

                        if (counter == 0)
                        {
                            await ctx.Channel.SendMessageAsync(embedBuilder.WithDescription($"There are no events registered for the year {Formatter.Underline(year)}."));
                        }

                        else if (counter == 1)
                        {
                            await ctx.Channel.SendMessageAsync(eventEmbeds.First().WithDescription($"List of all registered events for the year {Formatter.Underline(year)}. Indexed {counter} ({counter.ToWords()}) events."));
                        }

                        else
                        {
                            foreach (var embed in eventEmbeds)
                            {
                                embed.WithDescription($"List of all registered events for the year {Formatter.Underline(year)}. Indexed {counter} ({counter.ToWords()}) events. To navigate around the search results, interact with the buttons below.");
                            }

                            var pga = eventEmbeds.Select(x => new Page(string.Empty, x)).ToArray();

                            var interactivity = ctx.Client.GetInteractivity();
                            await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pga, PaginationBehaviour.WrapAround, ButtonPaginationBehavior.Disable);
                        }
                    });
                }
            }

            else
            {
                foreach (var component in firstRowButtons)
                {
                    component.Disable();
                }

                foreach (var component in secondRowButtons)
                {
                    component.Disable();
                }

                await message.ModifyAsync(x => x.WithEmbed(mainEmbed).AddComponents(firstRowButtons).AddComponents(secondRowButtons));

                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[TIMED OUT]")} {ctx.Member.Mention} You're taking too long to decide. Feel free to try again.");
            }
        }
        #endregion

        #region Functions
        /// <summary>
        /// Fetches the event data by the given name or ID. This function returns a single <see cref="Events" /> object once it matches with the given criteria.
        /// Not to be confused with indexing a list of events (<see cref="FetchAllEventsData(bool, string)" />) where this function returns more than one <see cref="Events" /> object.
        /// </summary>
        /// <param name="eventNameOrId">The name of the event or the row ID.</param>
        /// <param name="searchMode">The search strategy that tells how to search the event, when fetching via name.</param>
        /// <returns>An <see cref="Events" /> object.</returns>
        private Events FetchEventData(string keyword, EventSearchMode searchMode)
        {
            bool isNumber = int.TryParse(keyword, out int rowIDRaw);

            if (isNumber)
            {
                return _eventContext.Events.AsNoTracking().FirstOrDefault(x => x.Id == rowIDRaw);
            }

            else
            {
                if (searchMode is EventSearchMode.Exact)
                {
                    foreach (var eventData in _eventContext.Events.AsNoTracking())
                    {
                        if (eventData.EventName.ToLowerInvariant() == keyword.ToLowerInvariant())
                        {
                            return eventData;
                        }
                    }
                }

                else if (searchMode is EventSearchMode.ClosestMatching)
                {
                    foreach (var eventData in _eventContext.Events.AsNoTracking())
                    {
                        if (eventData.EventName.ToLowerInvariant().Contains(keyword.ToLowerInvariant()))
                        {
                            return eventData;
                        }
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
        /// <returns>An <see cref="IEnumerable{T}" /> of <see cref="Events" /> object.</returns>
        private IEnumerable<Events> FetchAllEventsData(bool indexYear, string keyword)
        {
            IEnumerable<Events> Events;
            List<Events> eventsData = new List<Events>();

            if (indexYear)
            {
                bool conversionSuccessful = int.TryParse(keyword, out int year);

                if (!conversionSuccessful)
                {
                    throw new Exception($"I can only accept years, not dates or words!");
                }

                foreach (var events in _eventContext.Events.AsNoTracking())
                {
                    DateTime eventDate = ClientUtilities.ConvertUnixTimestampToDateTime(events.EventDateUnixTimestamp);

                    if (year == eventDate.Year)
                    {
                        eventsData.Add(events);
                    }
                }
            }

            else
            {
                foreach (var events in _eventContext.Events.AsNoTracking())
                {
                    if (events.EventName.ToLowerInvariant().Contains(keyword.ToLowerInvariant()))
                    {
                        eventsData.Add(events);
                    }
                }
            }

            Events = eventsData;

            return Events;
        }

        /// <summary>
        /// Composes the embed field's value for the respective event.
        /// </summary>
        /// <returns>A string containing the details of the respective event.</returns>
        private string ComposeEventDescriptionField(Events events)
        {
            DateTime eventDate = ClientUtilities.ConvertUnixTimestampToDateTime(events.EventDateUnixTimestamp);

            bool isProposalEmpty = events.ProposalFileTitle is null ? false : true;

            if (DateTime.Now < eventDate)
            {
                TimeSpan remainingDateTime = eventDate - DateTime.Now;

                string reminderSettingStatus = events.ReminderDisabled is false ? "Enabled" : "Disabled";

                return $"Status: {ClientUtilities.ConvertBoolValue(events.Expired, ConvertBoolOption.UpcomingOrDone)} ({Formatter.Timestamp(remainingDateTime, TimestampFormat.RelativeTime)})\nKetua / Wakil Ketua Acara: {events.PersonInCharge}\nProposal: {ClientUtilities.ConvertBoolValue(isProposalEmpty, ConvertBoolOption.StoredOrNotStored)}\nReminders: {reminderSettingStatus}.\nDescription: {events.EventDescription}";
            }

            else
            {
                return $"Status: {ClientUtilities.ConvertBoolValue(events.Expired, ConvertBoolOption.UpcomingOrDone)}\nKetua / Wakil Ketua Acara: {events.PersonInCharge}\nProposal: {ClientUtilities.ConvertBoolValue(isProposalEmpty, ConvertBoolOption.StoredOrNotStored)}\nDescription: {events.EventDescription}";
            }
        }
        #endregion
    }
}
