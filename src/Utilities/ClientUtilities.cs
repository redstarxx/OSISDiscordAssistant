using System;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using DSharpPlus;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using OSISDiscordAssistant.Services;
using OSISDiscordAssistant.Enums;
using OSISDiscordAssistant.Constants;
using OSISDiscordAssistant.Models;
using Humanizer;

namespace OSISDiscordAssistant.Utilities
{
    public class ClientUtilities
    {
        /// <summary>
        /// Checks whether the respective CommandContext has either the Inti OSIS, Administrator or Service Administrator role.
        /// If false, error message is sent as reply.
        /// </summary>
        /// <returns>False if has none of the roles above.</returns>
        public static bool CheckAdminPermissions(CommandContext ctx)
        {
            // Role checks below.
            bool hasServiceAdminRole = CheckServiceAdminRole(ctx);

            bool hasAdminRole = ctx.Member.Roles.Any(x => x.Name == "Administrator");

            bool hasCoreCouncilRole = ctx.Member.Roles.Any(x => x.Name == "Inti OSIS");

            if (!hasServiceAdminRole)
            {
                if (!hasAdminRole)
                {
                    if (!hasCoreCouncilRole)
                    {
                        return false;
                    }

                    else
                    {
                        return true;
                    }
                }

                else
                {
                    return true;
                }
            }

            else
            {
                return true;
            }
        }

        /// <summary>
        /// Checks whether the member has the Service Administrator role.
        /// </summary>
        /// <returns>False if the Service Administrator role is not assigned.</returns>
        public static bool CheckServiceAdminRole(CommandContext ctx)
        {
            bool isServiceAdmin = false;

            isServiceAdmin = ctx.Member.Roles.Any(x => x.Name == "Service Administrator");

            return isServiceAdmin;
        }

        /// <summary>
        /// Checks whether the command invoker has the OSIS role assigned or not.
        /// </summary>
        /// <returns>False if the OSIS role is not assigned.</returns>
        public static bool CheckAccessRole(CommandContext ctx)
        {
            bool hasAccess = false;

            hasAccess = ctx.Member.Roles.Any(x => x.Name == "OSIS");

            return hasAccess;
        }

        /// <summary>
        /// Checks whether the command user is using the command towards themself.
        /// If true, errorReason is sent as reply.
        /// </summary>
        /// <param name="member">Targeted member for the command execution.</param>
        /// <param name="ctx">The respective CommandContext.</param>
        /// <returns>True if user is equals targeted user.</returns>
        public static async Task<bool> CheckSelfTargeting(DiscordMember member, CommandContext ctx)
        {
            if (ctx.User.Id == member.Id)
            {
                string errorReason = $"{Formatter.Bold("[ERROR]")} You cannot use this command on yourself.";
                await ctx.Channel.SendMessageAsync(errorReason).ConfigureAwait(false);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks whether the file name contains a file extension that is allowed.
        /// </summary>
        /// <param name="fileName">The file name to check.</param>
        /// <returns>True if the file name contains an allowed file extension.</returns>
        public static bool IsExtensionValid(string fileName)
        {
            string[] fileExtensions = 
            { 
                ".docx", ".docm", ".doc"
            };

            foreach (string extension in fileExtensions)
            {
                if (fileName.Contains(extension))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Parses a shortened timespan into seconds. Example: "1d16h35m". 
        /// </summary>
        /// <param name="input">String to parse into seconds.</param>
        /// <returns>TimeSpan in seconds.</returns>
        public static TimeSpan ParseToSeconds(string input)
        {
            var m = Regex.Match(input, @"^((?<days>\d+)d)?((?<hours>\d+)h)?((?<minutes>\d+)m)?((?<seconds>\d+)s)?$", RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.RightToLeft);

            int ds = m.Groups["days"].Success ? int.Parse(m.Groups["days"].Value) : 0;
            int hs = m.Groups["hours"].Success ? int.Parse(m.Groups["hours"].Value) : 0;
            int ms = m.Groups["minutes"].Success ? int.Parse(m.Groups["minutes"].Value) : 0;
            int ss = m.Groups["seconds"].Success ? int.Parse(m.Groups["seconds"].Value) : 0;

            TimeSpan timeSpan = TimeSpan.FromSeconds(ds * 86400 + hs * 60 * 60 + ms * 60 + ss);
            if (timeSpan.TotalSeconds is 0)
                throw new ArgumentException("Invalid time span string given.");

            return timeSpan;
        }

        /// <summary>
        /// Retrieves the role ID associated with the division name.
        /// </summary>
        /// <param name="divisionName">Name of the division to retrieve its role ID.</param>
        /// <returns>The role ID of the division name.</returns>
        public static ulong GetRoleID(string divisionName)
        {
            ulong roleID = 0;

            switch (divisionName.ToLowerInvariant())
            {
                case "inti":
                    roleID = 814450825702801421;
                    break;
                case "it":
                    roleID = 822423154864816138;
                    break;
                case "kesenian":
                    roleID = 822495032228708353;
                    break;
                case "kewirausahaan":
                    roleID = 822495096121327637;
                    break;
                case "olahraga":
                    roleID = 822495223694753852;
                    break;
                case "humas":
                    roleID = 823776027993833483;
                    break;
                case "agama":
                    roleID = 822495304300363796;
                    break;
                default:
                    break;
            }

            return roleID;
        }

        /// <summary>
        /// Converts the bool value to the specified option.
        /// </summary>
        /// <param name="boolValue">The bool value.</param>
        /// <param name="convertOption">The result string option.</param>
        /// <returns>A string based on the choosen ConvertBoolOption enum.</returns>
        public static string ConvertBoolValue(bool boolValue, ConvertBoolOption convertOption)
        {
            if (convertOption is ConvertBoolOption.ActiveOrDone)
            {
                switch (boolValue)
                {
                    case true:
                        return "Done.";
                    case false:
                        return "Active.";
                }
            }

            else if (convertOption is ConvertBoolOption.YesOrNo)
            {
                switch (boolValue)
                {
                    case true:
                        return "Yes.";
                    case false:
                        return "No.";
                }
            }

            else if (convertOption is ConvertBoolOption.StoredOrNotStored)
            {
                switch (boolValue)
                {
                    case true:
                        return "Stored.";
                    case false:
                        return "Not stored.";
                }
            }

            return null;
        }

        /// <summary>
        /// Composes a reminder message.
        /// </summary>
        /// <param name="remindMessage">Something to remind (text, link, picture, whatever).</param>
        /// <param name="ctx">The CommandContext to attach from.</param>
        /// <param name="remindTarget">The target mention string.</param>
        /// <returns></returns>
        public static string CreateReminderMessage(string remindMessage, CommandContext ctx, string remindTarget)
        {
            if (ctx.Member.Mention == remindTarget)
            {
                return $"{DiscordEmoji.FromName(ctx.Client, ":alarm_clock:")} {ctx.Member.Mention}, you wanted to be reminded of the following: \n\n{remindMessage}";
            }

            else
            {
                return $"{DiscordEmoji.FromName(ctx.Client, ":alarm_clock:")} {remindTarget}, {ctx.Member.Mention} wanted to remind you of the following: \n\n{remindMessage}";
            }
        }

        /// <summary>
        /// Composes a reminder receipt message (sent upon the completion of firing a reminder task).
        /// </summary>
        /// <param name="timeSpan">The timespan object.</param>
        /// <param name="remindMessage">Something to remind (text, link, picture, whatever).</param>
        /// <param name="displayTarget"></param>
        /// <returns></returns>
        public static string CreateReminderReceiptMessage(TimeSpan timeSpan, string remindMessage, string displayTarget)
        {
            return $"Okay! In {timeSpan.Humanize(1)} ({Formatter.Timestamp(timeSpan, TimestampFormat.LongDateTime)}) {displayTarget} will be reminded of the following:\n\n {remindMessage}";
        }

        /// <summary>
        /// Creates and fires a task which sends a reminder message after delaying from the specified timespan.
        /// </summary>
        /// <param name="remainingTime">The timespan object.</param>
        /// <param name="targetChannel">The DiscordChannel object that you want to send the reminder message to.</param>
        /// <param name="remindMessage">Something to remind (text, link, picture, whatever).</param>
        /// <param name="remindTarget">The target mention string.</param>
        public static void CreateReminderTask(TimeSpan remainingTime, DiscordChannel targetChannel, string remindMessage, CommandContext ctx, string remindTarget)
        {
            var reminderTask = new Task(async () =>
            {
                string reminderMessage = CreateReminderMessage(remindMessage, ctx, remindTarget);

                long fullDelays = remainingTime.Ticks / Constant.maxTimeSpanValue.Ticks;
                for (int i = 0; i < fullDelays; i++)
                {
                    await Task.Delay(Constant.maxTimeSpanValue);
                    remainingTime -= Constant.maxTimeSpanValue;
                }

                await Task.Delay(remainingTime);

                _ = targetChannel == ctx.Channel ? await ctx.RespondAsync(reminderMessage) : await targetChannel.SendMessageAsync(reminderMessage);
            });

            reminderTask.Start();
        }

        public static async void HandleVerificationRequests(DiscordClient client, ComponentInteractionCreateEventArgs e)
        {
            if (e.Id == "verify_button")
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

                var interactivity = client.GetInteractivity();

                var member = await e.Guild.GetMemberAsync(e.User.Id);

                bool verificationPending;
                bool isVerified = member.Roles.Any(x => x.Id == SharedData.AccessRoleId);

                using (var db = new VerificationContext())
                {
                    verificationPending = db.Verifications.Any(x => x.UserId == e.User.Id);
                }

                if (verificationPending)
                {
                    DiscordFollowupMessageBuilder followupMessageBuilder = new DiscordFollowupMessageBuilder()
                    {
                        Content = "You have already requested for a verification! If it has not been handled yet for some time, feel free to reach out to a member of Inti OSIS.",
                        IsEphemeral = true
                    };

                    await e.Interaction.CreateFollowupMessageAsync(followupMessageBuilder);

                    return;
                }

                if (isVerified)
                {
                    DiscordFollowupMessageBuilder followupMessageBuilder = new DiscordFollowupMessageBuilder()
                    {
                        Content = "You are already verified!",
                        IsEphemeral = true
                    };

                    await e.Interaction.CreateFollowupMessageAsync(followupMessageBuilder);

                    return;
                }

                else
                {
                    DiscordFollowupMessageBuilder followupMessageBuilder = new DiscordFollowupMessageBuilder()
                    {
                        Content = "Please check your DMs to proceed with your verification!",
                        IsEphemeral = true
                    };

                    await e.Interaction.CreateFollowupMessageAsync(followupMessageBuilder);
                }

                var askMessage = await member.SendMessageAsync($"Silahkan ketik nama panggilan anda. Contoh: {Formatter.InlineCode("Kerwintiro")}.");

                var nameInteractivity = await interactivity.WaitForMessageAsync(x => x.Channel is DiscordDmChannel && x.Author == e.User);

                if (nameInteractivity.Result.Content is not null)
                {
                    int verificationCounterNumber = 0;

                    using (var db = new CounterContext())
                    {
                        verificationCounterNumber = db.Counter.SingleOrDefault(x => x.Id == 1).VerifyCounter;

                        Counter rowToUpdate = null;
                        rowToUpdate = db.Counter.SingleOrDefault(x => x.Id == 1);

                        if (rowToUpdate != null)
                        {
                            rowToUpdate.VerifyCounter = verificationCounterNumber + 1;
                        }

                        db.SaveChanges();
                    }

                    var messageBuilder = new DiscordMessageBuilder();

                    //Sends a verification request to #verification as an embed.
                    var embedBuilder = new DiscordEmbedBuilder
                    {
                        Author = new DiscordEmbedBuilder.EmbedAuthor
                        {
                            Name = $"{e.User.Username}#{e.User.Discriminator}",
                            IconUrl = e.User.AvatarUrl
                        },
                        Title = $"Verification Request #{verificationCounterNumber} | PENDING",
                        Description = $"{e.User.Username}#{e.User.Discriminator} has submitted a verification request.\n"
                            + $"{Formatter.Bold("Requested Nickname:")} {nameInteractivity.Result.Content}\n{Formatter.Bold("User ID:")} {e.User.Id}\n{Formatter.Bold("Verification Status:")} PENDING.\n"
                            + $"Click the {Formatter.InlineCode("ACCEPT")} button to approve this request or the {Formatter.InlineCode("DECLINE")} button to deny.",
                        Timestamp = DateTime.Now,
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = "OSIS Discord Assistant"
                        },
                        Color = DiscordColor.MidnightBlue
                    };

                    messageBuilder.WithEmbed(embed: embedBuilder);
                    messageBuilder.AddComponents(new DiscordButtonComponent[]
                    {
                    new DiscordButtonComponent(ButtonStyle.Success, "accept_button", "ACCEPT", false, null),
                    new DiscordButtonComponent(ButtonStyle.Danger, "deny_button", "DECLINE", false, null)
                    });

                    DiscordChannel channel = e.Guild.GetChannel(SharedData.VerificationRequestsProcessingChannelId);
                    var requestEmbed = await channel.SendMessageAsync(builder: messageBuilder).ConfigureAwait(false);

                    using (var db = new VerificationContext())
                    {
                        db.Add(new Verification
                        {
                            UserId = e.User.Id,
                            VerificationEmbedId = requestEmbed.Id,
                            RequestedName = nameInteractivity.Result.Content
                        });

                        await db.SaveChangesAsync();
                    }

                    await member.SendMessageAsync($"Your verification request has been sent, {nameInteractivity.Result.Content}! Expect a response within the next 48 hours.");                  
                }               
            }

            else if (e.Id == "accept_button" || e.Id == "deny_button")
            {
                int verificationCounterNumber = 0;

                using (var db = new CounterContext())
                {
                    verificationCounterNumber = db.Counter.SingleOrDefault(x => x.Id == 1).VerifyCounter;
                }

                using (var db = new VerificationContext())
                {
                    if (e.Id == "accept_button")
                    {
                        var member = await e.Guild.GetMemberAsync(db.Verifications.SingleOrDefault(x => x.VerificationEmbedId == e.Message.Id).UserId);
                        await member.GrantRoleAsync(e.Guild.GetRole(SharedData.AccessRoleId));

                        await member.SendMessageAsync($"{Formatter.Bold("[VERIFICATION]")} Your verification request has been accepted by {e.User.Username}! You may now access the internal channels of {e.Guild.Name} and start chatting!");

                        var getEmbed = await e.Channel.GetMessageAsync(e.Message.Id);

                        DiscordEmbed updatedEmbed = null;

                        foreach (var embed in getEmbed.Embeds)
                        {
                            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder(embed)
                            {
                                Title = $"{embed.Title.Replace(" | PENDING", " | ACCEPTED")}",
                                Description = $"{member.Username}#{member.Discriminator} has submitted a verification request.\n"
                                + $"{Formatter.Bold("Requested Nickname:")} {db.Verifications.SingleOrDefault(x => x.VerificationEmbedId == e.Message.Id).RequestedName}\n{Formatter.Bold("User ID:")} {member.Id}\n{Formatter.Bold("Verification Status:")} ACCEPTED (handled by {e.Interaction.User.Mention} at <t:{e.Interaction.CreationTimestamp.ToUnixTimeSeconds()}:F>)."
                            };

                            updatedEmbed = embedBuilder.Build();

                            break;
                        };

                        var messageBuilder = new DiscordMessageBuilder()
                        {
                            Embed = updatedEmbed
                        };

                        await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder(messageBuilder));
                    }

                    else if (e.Id == "deny_button")
                    {
                        var member = await e.Guild.GetMemberAsync(db.Verifications.SingleOrDefault(x => x.VerificationEmbedId == e.Message.Id).UserId);
                        await member.SendMessageAsync($"{Formatter.Bold("[VERIFICATION]")} I'm sorry, your verification request has been {Formatter.Bold("DENIED")} by {e.User.Username}! You may reach out to a member of the Inti OSIS to find out why.");

                        var getEmbed = await e.Channel.GetMessageAsync(e.Message.Id);

                        DiscordEmbed updatedEmbed = null;

                        foreach (var embed in getEmbed.Embeds)
                        {
                            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder(embed)
                            {
                                Title = $"{embed.Title.Replace(" | PENDING", " | DENIED")}",
                                Description = $"{member.Username}#{member.Discriminator} has submitted a verification request.\n"
                                + $"{Formatter.Bold("Requested Nickname:")} {db.Verifications.SingleOrDefault(x => x.VerificationEmbedId == e.Message.Id).RequestedName}\n{Formatter.Bold("User ID:")} {member.Id}\n{Formatter.Bold("Verification Status:")} DENIED (handled by {e.Interaction.User.Mention} at <t:{e.Interaction.CreationTimestamp.ToUnixTimeSeconds()}:F>)."
                            };

                            updatedEmbed = embedBuilder.Build();

                            break;
                        };

                        var messageBuilder = new DiscordMessageBuilder()
                        {
                            Embed = updatedEmbed
                        };

                        await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder(messageBuilder));
                    }

                    // Remove the user data immediately, as it serves no purpose hereafter.
                    var rowToDelete = db.Verifications.SingleOrDefault(x => x.VerificationEmbedId == e.Message.Id);

                    db.Remove(rowToDelete);
                    await db.SaveChangesAsync();
                }
            }

            else if (e.Id == "why_button")
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

                DiscordFollowupMessageBuilder followupMessageBuilder = new DiscordFollowupMessageBuilder()
                {
                    Content = $"Although {e.Guild.Name} is a private server, a verification system is set up in the effort to deter server raids and keep unwanted users out, should the invite link is somehow compromised.\n\n" +
                    $"If you are a new member of OSIS Sekolah Djuwita Batam, get verified now to access our internal channels!",
                    IsEphemeral = true
                };

                await e.Interaction.CreateFollowupMessageAsync(followupMessageBuilder);
            }

            return;
        }

        /// <summary>
        /// An async version of reading the content of config.json which contains the connection string to the PostgreSQL database and deserializes it.
        /// </summary>
        /// <returns>The database connection string.</returns>
        public static async Task<string> GetConnectionStringAsync()
        {
            var json = string.Empty;
            using (var fileString = File.OpenRead("config.json"))
            using (var stringReader = new StreamReader(fileString, new UTF8Encoding(false)))
                json = await stringReader.ReadToEndAsync().ConfigureAwait(false);

            var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);

            string connectionString = configJson.ConnectionString;
            return connectionString;
        }

        /// <summary>
        /// Reads the content of config.json which contains the connection string to the PostgreSQL database and deserializes it.
        /// </summary>
        /// <returns>The database connection string.</returns>
        public static string GetConnectionString()
        {
            var json = string.Empty;
            using (var fileString = File.OpenRead("config.json"))
            using (var stringReader = new StreamReader(fileString, new UTF8Encoding(false)))
                json = stringReader.ReadToEnd();

            var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);

            string connectionString = configJson.ConnectionString;
            return connectionString;
        }

        /// <summary>
        /// Loads the main guild ID and channel IDs required for ERTask and PRTask to function properly.
        /// </summary>
        /// <param name="configJson">The JSON struct to read from.</param>
        public static void LoadDiscordConfigurationValues(ConfigJson configJson)
        {
            if (!configJson.MainGuildId.HasValue || !configJson.EventChannelId.HasValue || !configJson.ProposalChannelId.HasValue || 
                !configJson.VerificationRequestsCommandChannelId.HasValue || !configJson.VerificationRequestsProcessingChannelId.HasValue || 
                !configJson.ErrorChannelId.HasValue || !configJson.AccessRoleId.HasValue)
            {
                Console.WriteLine("One of the Discord ID values are missing. Terminating...");
                Console.ReadLine();

                Environment.Exit(0);
            }

            else
            {
                SharedData.MainGuildId = (ulong)configJson.MainGuildId;

                SharedData.EventChannelId = (ulong)configJson.EventChannelId;

                SharedData.ProposalChannelId = (ulong)configJson.ProposalChannelId;

                SharedData.VerificationRequestsCommandChannelId = (ulong)configJson.VerificationRequestsCommandChannelId;

                SharedData.VerificationRequestsProcessingChannelId = (ulong)configJson.VerificationRequestsProcessingChannelId;

                SharedData.ErrorChannelId = (ulong)configJson.ErrorChannelId;

                SharedData.AccessRoleId = (ulong)configJson.AccessRoleId;
            }
        }

        /// <summary>
        /// Gets the prefixes that are loaded from the config.json file.
        /// </summary>
        /// <returns>The list of prefixes formatted beautifully.</returns>
        public static string GetPrefixList()
        {
            int prefixCount = 0;
            int processedCount = 0;
            string prefixList = string.Empty;

            foreach (string prefix in SharedData.Prefixes)
            {
                prefixCount++;
            }

            foreach (string prefix in SharedData.Prefixes)
            {
                if (processedCount == 0)
                {
                    prefixList = $"{Formatter.InlineCode(prefix)}";
                    processedCount++;
                }

                else if (prefixCount - 1 == processedCount)
                {
                    prefixList = $"{prefixList} and {Formatter.InlineCode(prefix)}";
                    processedCount++;
                }

                else
                {
                    prefixList = $"{prefixList}, {Formatter.InlineCode(prefix)}";
                    processedCount++;
                }
            }

            return prefixList;
        }

        /// <summary>
        /// Gets the current version of the bot.
        /// </summary>
        /// <returns>The version number in major.minor.patch format.</returns>
        public static string GetBuildVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
        }
    }
}
