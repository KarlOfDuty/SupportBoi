using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
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

namespace SupportBoi.Commands;

[Command("admin")]
[Description("Administrative commands.")]
public class AdminCommands
{
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

        DiscordUser ticketUser = user == null ? command.User : user;

        long id = Database.NewTicket(ticketUser.Id, 0, command.Channel.Id);
        await command.RespondAsync(new DiscordEmbedBuilder
        {
            Color = DiscordColor.Green,
            Description = "Channel has been designated ticket " + id.ToString("00000") + "."
        });

        try
        {
            // Log it if the log channel exists
            DiscordChannel logChannel = await SupportBoi.client.GetChannelAsync(Config.logChannel);
            await logChannel.SendMessageAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = command.Channel.Mention + " has been designated ticket " + id.ToString("00000") + " by " + command.Member?.Mention + ".",
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "Ticket: " + id.ToString("00000")
                }
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
        [Parameter("ticket-id")] [Description("(Optional) Ticket to unset. Uses the channel you are in by default. Use ticket ID, not channel ID!")] long ticketID = 0)
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
                    Description = command.Channel.Mention + " has been undesignated as a ticket by " + command.User.Mention + ".",
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = "Ticket: " + ticket.id.ToString("00000")
                    }
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

        try
        {
            DiscordChannel logChannel = await SupportBoi.client.GetChannelAsync(Config.logChannel);
            await logChannel.SendMessageAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = command.Channel.Mention + " reloaded the bot.",
            });
        }
        catch (NotFoundException)
        {
            Logger.Error("Could not find the log channel.");
        }

        Logger.Log("Reloading bot...");
        await SupportBoi.Reload();
    }

    [RequireGuild]
    [Command("getinterviewtemplate")]
    [Description("Provides a copy of the interview template for a category which you can edit and then reupload.")]
    public async Task GetInterviewTemplate(SlashCommandContext command,
        [Parameter("category")] [Description("The category to get the template for.")] DiscordChannel category)
    {
        if (!category?.IsCategory ?? true)
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "That channel is not a category."
            }, true);
            return;
        }

        if (!Database.TryGetCategory(category.Id, out Database.Category categoryData))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "That category is not registered with the bot."
            }, true);
            return;
        }

        string interviewTemplateJSON = Database.GetInterviewTemplateJSON(category.Id);
        if (interviewTemplateJSON == null)
        {
            string defaultTemplate =
            "{\n" +
            "  \"category-id\": \"" + category.Id + "\",\n" +
            "  \"interview\":\n" +
            "  {\n" +
            "    \"message\": \"\",\n" +
            "    \"type\": \"\",\n" +
            "    \"color\": \"\",\n" +
            "    \"paths\":\n" +
            "    {\n" +
            "      \n" +
            "    }\n" +
            "  }\n" +
            "}";
            MemoryStream stream = new(Encoding.UTF8.GetBytes(defaultTemplate));

            DiscordInteractionResponseBuilder response = new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "No interview template found for this category. A default template has been generated."
            }).AddFile("interview-template-" + category.Id + ".json", stream).AsEphemeral();
            await command.RespondAsync(response);
        }
        else
        {
            MemoryStream stream = new(Encoding.UTF8.GetBytes(interviewTemplateJSON));
            await command.RespondAsync(new DiscordInteractionResponseBuilder().AddFile("interview-template-" + category.Id + ".json", stream).AsEphemeral());
        }
    }

    [RequireGuild]
    [Command("setinterviewtemplate")]
    [Description("Uploads an interview template file.")]
    public async Task SetInterviewTemplate(SlashCommandContext command,
        [Parameter("file")] [Description("The file containing the template.")] DiscordAttachment file)
    {
        await command.DeferResponseAsync(true);

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
            Interviews.ValidatedTemplate template = JsonConvert.DeserializeObject<Interviews.ValidatedTemplate>(json, new JsonSerializerSettings()
            {
                //NullValueHandling = NullValueHandling.Include,
                MissingMemberHandling = MissingMemberHandling.Error,
                Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
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
                    args.ErrorContext.Handled = false;
                }
            });

            DiscordChannel category = await SupportBoi.client.GetChannelAsync(template.categoryID);
            if (!category.IsCategory)
            {
                await command.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "The category ID in the uploaded JSON structure is not a valid category."
                }, true);
                return;
            }

            if (!Database.TryGetCategory(category.Id, out Database.Category _))
            {
                await command.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "The category ID in the uploaded JSON structure is not a category registered with the bot, use /addcategory first."
                }, true);
                return;
            }

            template.interview.Validate(ref errors, out int summaryCount, out int summaryMaxLength);

            if (summaryCount > 25)
            {
                errors.Add("A summary cannot contain more than 25 fields, but you have " + summaryCount + " fields in at least one of your interview branches.");
            }

            if (summaryMaxLength >= 6000)
            {
                errors.Add("A summary cannot contain more than 6000 characters, but at least one of your branches has the possibility of its summary reaching " + summaryMaxLength + " characters.");
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

            if (!Database.SetInterviewTemplate(template))
            {
                await command.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "An error occured trying to write the new template to database."
                }, true);
                return;
            }

            try
            {
                MemoryStream memStream = new(Encoding.UTF8.GetBytes(Database.GetInterviewTemplateJSON(template.categoryID)));

                // Log it if the log channel exists
                DiscordChannel logChannel = await SupportBoi.client.GetChannelAsync(Config.logChannel);
                await logChannel.SendMessageAsync(new DiscordMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Green,
                    Description = command.User.Mention + " uploaded a new interview template for the `" + category.Name +"` category."
                }).AddFile("interview-template-" + template.categoryID + ".json", memStream));
            }
            catch (NotFoundException)
            {
                Logger.Error("Could not find the log channel.");
            }
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