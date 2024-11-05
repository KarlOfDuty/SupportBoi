using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;

namespace SupportBoi.Commands;

[Command("admin")]
[Description("Administrative commands.")]
public class AdminCommands
{
    [RequireGuild]
    [Command("listinvalid")]
    [Description("List tickets which channels have been deleted. Use /admin unsetticket <id> to remove them.")]
    public async Task ListInvalid(SlashCommandContext command)
    {
        if (!Database.TryGetOpenTickets(out List<Database.Ticket> openTickets))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Could not get any open tickets from database."
            }, true);
        }

        // Get all channels in all guilds the bot is part of
        List<DiscordChannel> allChannels = new List<DiscordChannel>();
        foreach (KeyValuePair<ulong,DiscordGuild> guild in SupportBoi.client.Guilds)
        {
            try
            {
                allChannels.AddRange(await guild.Value.GetChannelsAsync());
            }
            catch (Exception) { /*ignored*/ }
        }

        // Check which tickets channels no longer exist
        List<string> listItems = new List<string>();
        foreach (Database.Ticket ticket in openTickets)
        {
            if (allChannels.All(channel => channel.Id != ticket.channelID))
            {
                listItems.Add("ID: **" + ticket.id.ToString("00000") + ":** <#" + ticket.channelID + ">\n");
            }
        }

        if (listItems.Count == 0)
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "All tickets are valid!"
            }, true);
            return;
        }

        List<DiscordEmbedBuilder> embeds = new List<DiscordEmbedBuilder>();
        foreach (string message in Utilities.ParseListIntoMessages(listItems))
        {
            embeds.Add(new DiscordEmbedBuilder
            {
                Title = "Invalid tickets:",
                Color = DiscordColor.Red,
                Description = message
            });
        }

        // Add the footers
        for (int i = 0; i < embeds.Count; i++)
        {
            embeds[i].Footer = new DiscordEmbedBuilder.EmbedFooter
            {
                Text = $"Page {i + 1} / {embeds.Count}"
            };
        }

        List<Page> listPages = new List<Page>();
        foreach (DiscordEmbedBuilder embed in embeds)
        {
            listPages.Add(new Page("", embed));
        }

        await command.Interaction.SendPaginatedResponseAsync(true, command.User, listPages);
    }

    [RequireGuild]
    [Command("setticket")]
    [Description("Turns a channel into a ticket. WARNING: Anyone will be able to delete the channel using /close.")]
    public async Task SetTicket(SlashCommandContext command,
        [Parameter("user")] [Description("(Optional) The owner of the ticket.")] DiscordUser user = null)
    {
        // Check if ticket exists in the database
        if (Database.IsOpenTicket(command.Channel.Id))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "This channel is already a ticket."
            }, true);
            return;
        }

        DiscordUser ticketUser = (user == null ? command.User : user);

        long id = Database.NewTicket(ticketUser.Id, 0, command.Channel.Id);
        string ticketID = id.ToString("00000");
        await command.RespondAsync(new DiscordEmbedBuilder
        {
            Color = DiscordColor.Green,
            Description = "Channel has been designated ticket " + ticketID + "."
        });

        try
        {
            // Log it if the log channel exists
            DiscordChannel logChannel = await SupportBoi.client.GetChannelAsync(Config.logChannel);
            await logChannel.SendMessageAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = command.Channel.Mention + " has been designated ticket " + ticketID + " by " + command.Member.Mention + "."
            });
        }
        catch (NotFoundException)
        {
            Logger.Error("Could not find the log channel.");
        }
    }

    [RequireGuild]
    [Command("unsetticket")]
    [Description("Deletes a ticket from the ticket system without deleting the channel.")]
    public async Task UnsetTicket(SlashCommandContext command,
        [Parameter("ticket-id")] [Description("(Optional) Ticket to unset. Uses the channel you are in by default.")] long ticketID = 0)
    {
        Database.Ticket ticket;

        if (ticketID == 0)
        {
            // Check if ticket exists in the database
            if (!Database.TryGetOpenTicket(command.Channel.Id, out ticket))
            {
                await command.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "This channel is not a ticket!"
                }, true);
                return;
            }
        }
        else
        {
            // Check if ticket exists in the database
            if (!Database.TryGetOpenTicketByID((uint)ticketID, out ticket))
            {
                await command.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "There is no ticket with this ticket ID."
                }, true);
                return;
            }
        }


        if (Database.DeleteOpenTicket(ticket.id))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "Channel has been undesignated as a ticket."
            });

            try
            {
                // Log it if the log channel exists
                DiscordChannel logChannel = await SupportBoi.client.GetChannelAsync(Config.logChannel);
                await logChannel.SendMessageAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Green,
                    Description = command.Channel.Mention + " has been undesignated as a ticket by " + command.Member.Mention + "."
                });
            }
            catch (NotFoundException)
            {
                Logger.Error("Could not find the log channel.");
            }
        }
        else
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Error: Failed removing ticket from database."
            }, true);
        }
    }

    [RequireGuild]
    [Command("reload")]
    [Description("Reloads the bot config.")]
    public async Task Reload(SlashCommandContext command)
    {
        await command.RespondAsync(new DiscordEmbedBuilder
        {
            Color = DiscordColor.Green,
            Description = "Reloading bot application..."
        });
        Logger.Log("Reloading bot...");
        await SupportBoi.Reload();
    }

    [RequireGuild]
    [Command("getinterviewtemplates")]
    [Description("Provides a copy of the interview templates which you can edit and then reupload.")]
    public async Task GetInterviewTemplates(SlashCommandContext command)
    {
        MemoryStream stream = new(Encoding.UTF8.GetBytes(Database.GetInterviewTemplatesJSON()));
        await command.RespondAsync(new DiscordInteractionResponseBuilder().AddFile("interview-templates.json", stream).AsEphemeral());
    }

    [RequireGuild]
    [Command("setinterviewtemplates")]
    [Description("Uploads an interview template file.")]
    public async Task SetInterviewTemplates(SlashCommandContext command, [Parameter("file")] DiscordAttachment file)
    {
        if (!file.MediaType?.Contains("application/json") ?? false)
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "The uploaded file is not a JSON file according to Discord."
            }, true);
            return;
        }

        Stream stream = await new HttpClient().GetStreamAsync(file.Url);
        string json = await new StreamReader(stream).ReadToEndAsync();

        try
        {
            List<string> errors = [];

            // Convert it to an interview object to validate the template
            Dictionary<ulong, Interviewer.ValidatedInterviewQuestion> interview = JsonConvert.DeserializeObject<Dictionary<ulong, Interviewer.ValidatedInterviewQuestion>>(json, new JsonSerializerSettings()
            {
                //NullValueHandling = NullValueHandling.Include,
                MissingMemberHandling = MissingMemberHandling.Error,
                Error = delegate (object sender, ErrorEventArgs args)
                {
                    // I noticed the main exception mainly has information for developers, not administrators,
                    // so I switched to using the inner message if available.
                    if (string.IsNullOrEmpty(args.ErrorContext.Error.InnerException?.Message))
                    {
                        errors.Add(args.ErrorContext.Error.Message);
                    }
                    else
                    {
                        errors.Add(args.ErrorContext.Error.InnerException.Message);
                    }

                    Logger.Debug("Exception occured when trying to upload interview template:\n" + args.ErrorContext.Error);
                    args.ErrorContext.Handled = true;
                }
            });

            if (interview != null)
            {
                foreach (KeyValuePair<ulong, Interviewer.ValidatedInterviewQuestion> interviewRoot in interview)
                {
                    interviewRoot.Value.Validate(ref errors, out int summaryCount, out int summaryMaxLength);

                    if (summaryCount > 25)
                    {
                        errors.Add("A summary cannot contain more than 25 fields, but you have " + summaryCount + " fields in one of your interview branches.");
                    }

                    if (summaryMaxLength >= 6000)
                    {
                        errors.Add("A summary cannot contain more than 6000 characters, but one of your branches has the possibility of its summary reaching " + summaryMaxLength + " characters.");
                    }
                }
            }

            if (errors.Count != 0)
            {
                string errorString = string.Join("\n\n", errors);
                if (errorString.Length > 1500)
                {
                    errorString = errorString.Substring(0, 1500);
                }

                await command.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "The uploaded JSON structure could not be converted to an interview template.\n\nErrors:\n```\n" + errorString + "\n```",
                    Footer = new DiscordEmbedBuilder.EmbedFooter()
                    {
                        Text = "More detailed information may be available as debug messages in the bot logs."
                    }
                }, true);
                return;
            }

            Database.SetInterviewTemplates(JsonConvert.SerializeObject(interview, Formatting.Indented));
        }
        catch (Exception e)
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {

                Color = DiscordColor.Red,
                Description = "The uploaded JSON structure could not be converted to an interview template.\n\nError message:\n```\n" + e.Message + "\n```"
            }, true);
            return;
        }

        await command.RespondAsync(new DiscordEmbedBuilder
        {

            Color = DiscordColor.Green,
            Description = "Uploaded interview template."
        }, true);
    }
}