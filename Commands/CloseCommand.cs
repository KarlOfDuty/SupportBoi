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
        ulong channelID = interaction.Channel.Id;
        string channelName = interaction.Channel.Name;

        // Check if ticket exists in the database
        if (!Database.TryGetOpenTicket(channelID, out Database.Ticket ticket))
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
        if (closeReasons.TryGetValue(channelID, out string cachedReason))
        {
            closeReason = "\nReason: " + cachedReason + "\n";
        }

        // TODO: This throws an exception instead of returning null now

        // Log it if the log channel exists
        DiscordChannel logChannel = await interaction.Guild.GetChannelAsync(Config.logChannel);
        if (logChannel != null)
        {
            DiscordEmbed embed = new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "Ticket " + ticket.id.ToString("00000") + " closed by " +
                              interaction.User.Mention + ".\n" + closeReason,
                Footer = new DiscordEmbedBuilder.EmbedFooter { Text = '#' + channelName }
            };

            await using FileStream file = new FileStream(Transcriber.GetPath(ticket.id), FileMode.Open, FileAccess.Read);
            DiscordMessageBuilder message = new DiscordMessageBuilder();
            message.AddEmbed(embed);
            message.AddFiles(new Dictionary<string, Stream> { { Transcriber.GetFilename(ticket.id), file } });

            await logChannel.SendMessageAsync(message);
        }

        if (Config.closingNotifications)
        {
            DiscordEmbed embed = new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "Ticket " + ticket.id.ToString("00000") + " which you opened has now been closed, " +
                              "check the transcript for more info.\n" + closeReason,
                Footer = new DiscordEmbedBuilder.EmbedFooter { Text = '#' + channelName }
            };

            try
            {
                // TODO: This throws an exception instead of returning null now
                DiscordMember staffMember = await interaction.Guild.GetMemberAsync(ticket.creatorID);
                await using FileStream file = new FileStream(Transcriber.GetPath(ticket.id), FileMode.Open, FileAccess.Read);

                DiscordMessageBuilder message = new DiscordMessageBuilder();
                message.AddEmbed(embed);
                message.AddFiles(new Dictionary<string, Stream> { { Transcriber.GetFilename(ticket.id), file } });

                await staffMember.SendMessageAsync(message);
            }
            catch (NotFoundException) { }
            catch (UnauthorizedException) { }
        }

        Database.ArchiveTicket(ticket);

        await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
        {
            Color = DiscordColor.Green,
            Description = "Channel will be deleted in 3 seconds..."
        }));


        await Task.Delay(3000);

        // Delete the channel and database entry
        await interaction.Channel.DeleteAsync("Ticket closed.");

        Database.DeleteOpenTicket(ticket.id);

        closeReasons.Remove(channelID);
    }
}