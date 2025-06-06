﻿using System;
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
            if (Database.Ticket.TryGetOpenTicket(command.Channel.Id, out ticket))
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
            if (Database.Ticket.TryGetOpenTicketByID((uint)ticketID, out ticket) && ticket?.creatorID == command.Member.Id)
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
            else if (!Database.Ticket.TryGetClosedTicket((uint)ticketID, out ticket) || (ticket?.creatorID != command.Member.Id && !Database.StaffMember.IsStaff(command.Member.Id)))
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
        bool zipTooLarge = false;

        // If the zip transcript doesn't exist or is too large, use the html file.
        try
        {
            FileInfo zipFile = new(filePath);
            if (!zipFile.Exists || zipFile.Length >= 26214400)
            {
                fileName = Transcriber.GetHTMLFilename(ticket.id);
                filePath = Transcriber.GetHtmlPath(ticket.id);
            }

            if (zipFile.Exists && zipFile.Length >= 26214400)
            {
                zipTooLarge = true;
            }
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
            await using FileStream file = new(filePath, FileMode.Open, FileAccess.Read);
            await LogChannel.Success("Transcript generated by " + command.User.Mention + ".", ticket.id, new Utilities.File(fileName, file));
        }
        catch (Exception e)
        {
            Logger.Error("Failed to log transcript generation.", e);
        }

        try
        {
            // Send transcript in a direct message
            await using FileStream file = new(filePath, FileMode.Open, FileAccess.Read);

            DiscordMessageBuilder directMessage = new();

            if (zipTooLarge)
            {
                directMessage.AddEmbed(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Orange,
                    Description = "Transcript generated.\n\nThe zip file is too large, sending only the HTML file. Ask an administrator for the zip if you need it.",
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = "Ticket: " + ticket.id.ToString("00000")
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
                        Text = "Ticket: " + ticket.id.ToString("00000")
                    }
                });
            }

            directMessage.AddFiles(new Dictionary<string, Stream> { { fileName, file } });

            await command.Member.SendMessageAsync(directMessage);

            await command.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "Transcript sent!\n"
            }));
        }
        catch (UnauthorizedException)
        {
            await command.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Not allowed to send direct message to you, please check your privacy settings.\n"
            }));
        }
    }
}