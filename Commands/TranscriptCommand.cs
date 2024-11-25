using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

namespace SupportBoi.Commands;

public class TranscriptCommand
{
    // TODO: Refactor the hell out of this
    [RequireGuild]
    [Command("transcript")]
    [Description("Creates a transcript of a ticket.")]
    public async Task OnExecute(SlashCommandContext command, [Parameter("ticket-id")] [Description("(Optional) Ticket number to get transcript of.")] long ticketID = 0)
    {
        await command.DeferResponseAsync(true);
        Database.Ticket ticket;
        if (ticketID == 0) // If there are no arguments use current channel
        {
            if (Database.TryGetOpenTicket(command.Channel.Id, out ticket))
            {
                try
                {
                    await Transcriber.ExecuteAsync(command.Channel.Id, ticket.id);
                }
                catch (Exception)
                {
                    await command.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Red,
                        Description = "ERROR: Could not save transcript file. Aborting..."
                    }));
                    throw;
                }
            }
            else
            {
                await command.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "This channel is not a ticket."
                }));
                return;
            }
        }
        else
        {
            // If the ticket is still open, generate a new fresh transcript
            if (Database.TryGetOpenTicketByID((uint)ticketID, out ticket) && ticket?.creatorID == command.Member.Id)
            {
                try
                {
                    await Transcriber.ExecuteAsync(command.Channel.Id, ticket.id);
                }
                catch (Exception)
                {
                    await command.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Red,
                        Description = "ERROR: Could not save transcript file. Aborting..."
                    }));
                    throw;
                }

            }
            // If there is no open or closed ticket, send an error. If there is a closed ticket we will simply use the old transcript from when the ticket was closed.
            else if (!Database.TryGetClosedTicket((uint)ticketID, out ticket) || (ticket?.creatorID != command.Member.Id && !Database.IsStaff(command.Member.Id)))
            {
                await command.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "Could not find a closed ticket with that number which you opened.\n(Use the /list command to see all your tickets)"
                }));
                return;
            }
        }

        string fileName = Transcriber.GetZipFilename(ticket.id);
        string filePath = Transcriber.GetZipPath(ticket.id);
        long zipSize = 0;

        // If the zip transcript doesn't exist, use the html file.
        try
        {
            FileInfo fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists || fileInfo.Length >= 26214400)
            {
                fileName = Transcriber.GetHTMLFilename(ticket.id);
                filePath = Transcriber.GetHtmlPath(ticket.id);
            }
            zipSize = fileInfo.Length;
        }
        catch (Exception e)
        {
            await command.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "ERROR: Could not find transcript file. Aborting..."
            }));
            Logger.Error("Failed to access transcript file:", e);
            return;
        }

        // Check if the chosen file path works.
        if (!File.Exists(filePath))
        {
            await command.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "ERROR: Could not find transcript file. Aborting..."
            }));
            Logger.Error("Transcript file does not exist: \"" + filePath + "\"");
            return;
        }

        try
        {
            // Log it if the log channel exists
            DiscordChannel logChannel = await SupportBoi.client.GetChannelAsync(Config.logChannel);
            await using FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read);

            DiscordMessageBuilder message = new DiscordMessageBuilder();
            message.AddEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "Transcript generated by " + command.User.Mention + ".",
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "Ticket: " + ticket.id.ToString("00000")
                }
            });
            message.AddFiles(new Dictionary<string, Stream> { { fileName, file } });

            await logChannel.SendMessageAsync(message);
        }
        catch (NotFoundException)
        {
            Logger.Error("Could not send message in log channel.");
        }

        if (await SendDirectMessage(command, fileName, filePath, zipSize, ticket.id))
        {
            await command.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "Transcript sent!\n"
            }));
        }
    }

    private static async Task<bool> SendDirectMessage(SlashCommandContext command, string fileName, string filePath, long zipSize, uint ticketID)
    {
        try
        {
            // Send transcript in a direct message
            await using FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read);

            DiscordMessageBuilder directMessage = new DiscordMessageBuilder();

            if (zipSize >= 26214400)
            {
                directMessage.AddEmbed(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Orange,
                    Description = "Transcript generated.\n\nThe zip file is too large, sending only the HTML file. Ask an administrator for the zip if you need it.",
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = "Ticket: " + ticketID.ToString("00000")
                    }
                });
            }
            else
            {
                directMessage.AddEmbed(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Green,
                    Description = "Transcript generated!\n",
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = "Ticket: " + ticketID.ToString("00000")
                    }
                });
            }

            directMessage.AddFiles(new Dictionary<string, Stream> { { fileName, file } });

            await command.Member.SendMessageAsync(directMessage);
            return true;
        }
        catch (UnauthorizedException)
        {
            await command.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Not allowed to send direct message to you, please check your privacy settings.\n"
            }));
            return false;
        }
    }
}