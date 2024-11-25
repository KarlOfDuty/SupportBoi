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
        closeReasons.Add(command.Channel.Id, reason);
    }

    public static async Task OnConfirmed(DiscordInteraction interaction)
    {
        await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

        // Check if ticket exists in the database
        if (!Database.TryGetOpenTicket(interaction.Channel.Id, out Database.Ticket ticket))
        {
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
            FileInfo fi = new FileInfo(filePath);
            if (!fi.Exists || fi.Length >= 26214400)
            {
                fileName = Transcriber.GetHTMLFilename(ticket.id);
                filePath = Transcriber.GetHtmlPath(ticket.id);
            }
            zipSize = fi.Length;
        }
        catch (Exception e)
        {
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
            // Log it if the log channel exists
            DiscordChannel logChannel = await SupportBoi.client.GetChannelAsync(Config.logChannel);

            await using FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            DiscordMessageBuilder message = new DiscordMessageBuilder();
            message.AddEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "Ticket " + ticket.id.ToString("00000") + " closed by " +
                              interaction.User.Mention + ".\n" + closeReason,
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

        if (Config.closingNotifications)
        {
            try
            {
                DiscordUser staffMember = await SupportBoi.client.GetUserAsync(ticket.creatorID);
                await using FileStream file = new(filePath, FileMode.Open, FileAccess.Read);

                DiscordMessageBuilder message = new();

                if (zipSize >= 26214400)
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

                await staffMember.SendMessageAsync(message);
            }
            catch (NotFoundException) { }
            catch (UnauthorizedException) { }
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
    }
}