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

public class CloseCommand
{
    // TODO: Refactor this class a whole lot
    private static Dictionary<ulong, string> closeReasons = new Dictionary<ulong, string>();
    private static List<ulong> currentlyClosingTickets = new List<ulong>();

    private const int MAX_UPLOAD_SIZE = 10 * 1024 * 1024;

    [RequireGuild]
    [Command("close")]
    [Description("Closes this ticket.")]
    public async Task OnExecute(SlashCommandContext command,
        [Parameter("reason")] [Description("(Optional) The reason for closing this ticket.")] string reason = "")
    {
        // Check if ticket exists in the database
        if (!Database.TryGetOpenTicket(command.Channel.Id, out Database.Ticket _))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "This channel is not a ticket."
            });
            return;
        }

        DiscordInteractionResponseBuilder confirmation = new DiscordInteractionResponseBuilder()
            .AddEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Cyan,
                Description = "Are you sure you wish to close this ticket? You cannot re-open it again later."
            })
            .AddComponents(new DiscordButtonComponent(DiscordButtonStyle.Danger, "supportboi_closeconfirm", "Confirm"));

        await command.RespondAsync(confirmation);

        if (closeReasons.TryGetValue(command.Channel.Id, out _))
        {
            closeReasons[command.Channel.Id] = reason;
        }
        else
        {
            closeReasons.Add(command.Channel.Id, reason);
        }
    }

    public static async Task OnConfirmed(DiscordInteraction interaction)
    {
        if (currentlyClosingTickets.Contains(interaction.Channel.Id))
        {
            await interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                                                  new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "This ticket is already closing."
            }).AsEphemeral());
            return;
        }

        try
        {
            currentlyClosingTickets.Add(interaction.Channel.Id);
            await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

            // Check if ticket exists in the database
            if (!Database.TryGetOpenTicket(interaction.Channel.Id, out Database.Ticket ticket))
            {
                currentlyClosingTickets.Remove(interaction.Channel.Id);
                await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "This channel is not a ticket."
                }));
                return;
            }

            // Build transcript
            try
            {
                await Transcriber.ExecuteAsync(interaction.Channel.Id, ticket.id);
            }
            catch (Exception e)
            {
                currentlyClosingTickets.Remove(interaction.Channel.Id);
                Logger.Error("Exception occured when trying to save transcript while closing ticket: " + e);
                await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "ERROR: Could not save transcript file. Aborting..."
                }));
                return;
            }

            string closeReason = "";
            if (closeReasons.TryGetValue(interaction.Channel.Id, out string cachedReason))
            {
                closeReason = "\nReason: " + cachedReason + "\n";
            }

            string fileName = Transcriber.GetZipFilename(ticket.id);
            string filePath = Transcriber.GetZipPath(ticket.id);
            long zipSize = 0;

            // If the zip transcript doesn't exist, use the html file.
            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists || fileInfo.Length >= MAX_UPLOAD_SIZE)
                {
                    fileName = Transcriber.GetHTMLFilename(ticket.id);
                    filePath = Transcriber.GetHtmlPath(ticket.id);
                    Logger.Debug("Transcript archive too large, sending only html instead.");
                }
                zipSize = fileInfo.Length;
                Logger.Debug("Transcript zip size: " + zipSize + " bytes.");
            }
            catch (Exception e)
            {
                currentlyClosingTickets.Remove(interaction.Channel.Id);
                await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
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
                currentlyClosingTickets.Remove(interaction.Channel.Id);
                await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "ERROR: Could not find transcript file. Aborting..."
                }));
                Logger.Error("Transcript file does not exist: \"" + filePath + "\"");
                return;
            }

            try
            {
                await using FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                await LogChannel.Success("Ticket " + ticket.id.ToString("00000") + " closed by " + interaction.User.Mention + ".\n" + closeReason, ticket.id, new Utilities.File(fileName, file));
            }
            catch (Exception e)
            {
                Logger.Error("Error occurred sending transcript log message. ", e);
            }

            if (Config.closingNotifications)
            {
                try
                {
                    DiscordUser ticketCreator = await SupportBoi.client.GetUserAsync(ticket.creatorID);
                    await using FileStream file = new(filePath, FileMode.Open, FileAccess.Read);

                    DiscordMessageBuilder message = new();

                    if (zipSize >= MAX_UPLOAD_SIZE)
                    {
                        message.AddEmbed(new DiscordEmbedBuilder
                        {
                            Color = DiscordColor.Orange,
                            Description = "Ticket " + ticket.id.ToString("00000") + " which you opened has now been closed, check the transcript for more info.\n\n" +
                                          "The zip file is too large, sending only the HTML file. Ask an administrator for the zip if you need it.\"\n" + closeReason,
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = "Ticket: " + ticket.id.ToString("00000")
                            }
                        });
                    }
                    else
                    {
                        message.AddEmbed(new DiscordEmbedBuilder
                        {
                            Color = DiscordColor.Green,
                            Description = "Ticket " + ticket.id.ToString("00000") + " which you opened has now been closed, " + "check the transcript for more info.\n" + closeReason,
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = "Ticket: " + ticket.id.ToString("00000")
                            }
                        });
                    }

                    message.AddFiles(new Dictionary<string, Stream> { { fileName, file } });

                    await ticketCreator.SendMessageAsync(message);
                }
                catch (NotFoundException) { /* ignore */ }
                catch (UnauthorizedException) { /* ignore */ }
            }

            Database.ArchiveTicket(ticket);
            Database.TryDeleteInterview(interaction.Channel.Id);

            await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "Channel will be deleted in 3 seconds..."
            }));

            await Task.Delay(3000);

            // Delete the channel and database entry
            await interaction.Channel.DeleteAsync("Ticket closed.");

            Database.DeleteOpenTicket(ticket.id);

            closeReasons.Remove(interaction.Channel.Id);
            currentlyClosingTickets.Remove(interaction.Channel.Id);
        }
        catch (Exception e)
        {
            currentlyClosingTickets.Remove(interaction.Channel.Id);
            await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "An unexpected error occurred when trying to close ticket. Aborting..."
            }));
            Logger.Error("An unexpected error occurred when trying to close ticket:", e);
        }
    }
}