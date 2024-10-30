using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DiscordChatExporter.Core.Utils.Extensions;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.EventArgs;
using DSharpPlus.Commands.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using SupportBoi.Commands;

namespace SupportBoi;

public static class EventHandler
{
    public static Task OnReady(DiscordClient client, GuildDownloadCompletedEventArgs e)
    {
        Logger.Log("Connected to Discord.");

        // Checking activity type
        if (!Enum.TryParse(Config.presenceType, true, out DiscordActivityType activityType))
        {
            Logger.Log("Presence type '" + Config.presenceType + "' invalid, using 'Playing' instead.");
            activityType = DiscordActivityType.Playing;
        }

        client.UpdateStatusAsync(new DiscordActivity(Config.presenceText, activityType), DiscordUserStatus.Online);
        return Task.CompletedTask;
    }

    public static async Task OnGuildAvailable(DiscordClient discordClient, GuildAvailableEventArgs e)
    {
        Logger.Log("Found Discord server: " + e.Guild.Name + " (" + e.Guild.Id + ")");

        if (SupportBoi.commandLineArgs.serversToLeave.Contains(e.Guild.Id))
        {
            Logger.Warn("LEAVING DISCORD SERVER AS REQUESTED: " + e.Guild.Name + " (" + e.Guild.Id + ")");
            await e.Guild.LeaveAsync();
            return;
        }

        IReadOnlyDictionary<ulong, DiscordRole> roles = e.Guild.Roles;

        foreach ((ulong roleID, DiscordRole role) in roles)
        {
            Logger.Debug(role.Name.PadRight(40, '.') + roleID);
        }
    }

    public static async Task OnMessageCreated(DiscordClient client, MessageCreatedEventArgs e)
    {
        if (e.Author.IsBot)
        {
            return;
        }

        // Check if ticket exists in the database and ticket notifications are enabled
        if (!Database.TryGetOpenTicket(e.Channel.Id, out Database.Ticket ticket) || !Config.ticketUpdatedNotifications)
        {
            return;
        }

        // Ignore staff messages
        if (Database.IsStaff(e.Author.Id))
        {
            return;
        }

        // Sends a DM to the assigned staff member if at least a day has gone by since the last message
        IReadOnlyList<DiscordMessage> messages = await e.Channel.GetMessagesAsync(2);
        if (messages.Count > 1 && messages[1].Timestamp < DateTimeOffset.UtcNow.AddDays(Config.ticketUpdatedNotificationDelay * -1))
        {
            try
            {
                DiscordMember staffMember = await e.Guild.GetMemberAsync(ticket.assignedStaffID);
                await staffMember.SendMessageAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Green,
                    Description = "A ticket you are assigned to has been updated: " + e.Channel.Mention
                });
            }
            catch (NotFoundException) { }
            catch (UnauthorizedException) { }
        }
    }

    public static async Task OnMemberAdded(DiscordClient client, GuildMemberAddedEventArgs e)
    {
        if (!Database.TryGetOpenTickets(e.Member.Id, out List<Database.Ticket> ownTickets))
        {
            return;
        }

        foreach (Database.Ticket ticket in ownTickets)
        {
            try
            {
                DiscordChannel channel = await client.GetChannelAsync(ticket.channelID);
                if (channel?.GuildId == e.Guild.Id)
                {
                    try
                    {
                        await channel.AddOverwriteAsync(e.Member, DiscordPermissions.AccessChannels);
                        await channel.SendMessageAsync(new DiscordEmbedBuilder
                        {
                            Color = DiscordColor.Green,
                            Description = "User '" + e.Member.Username + "#" + e.Member.Discriminator + "' has rejoined the server, and has been re-added to the ticket."
                        });
                    }
                    catch (DiscordException ex)
                    {
                        Logger.Error("Exception occurred trying to add channel permissions: " + ex);
                        Logger.Error("JsomMessage: " + ex.JsonMessage);
                    }

                }
            }
            catch (Exception) { /* ignored */ }
        }
    }

    public static async Task OnMemberRemoved(DiscordClient client, GuildMemberRemovedEventArgs e)
    {
        if (Database.TryGetOpenTickets(e.Member.Id, out List<Database.Ticket> ownTickets))
        {
            foreach(Database.Ticket ticket in ownTickets)
            {
                try
                {
                    DiscordChannel channel = await client.GetChannelAsync(ticket.channelID);
                    if (channel?.GuildId == e.Guild.Id)
                    {
                        await channel.SendMessageAsync(new DiscordEmbedBuilder
                        {
                            Color = DiscordColor.Red,
                            Description = "User '" + e.Member.Username + "#" + e.Member.Discriminator + "' has left the server."
                        });
                    }
                }
                catch (Exception) { /* ignored */ }
            }
        }

        if (Database.TryGetAssignedTickets(e.Member.Id, out List<Database.Ticket> assignedTickets) && Config.logChannel != 0)
        {
            DiscordChannel logChannel = await client.GetChannelAsync(Config.logChannel);
            if (logChannel != null)
            {
                foreach (Database.Ticket ticket in assignedTickets)
                {
                    try
                    {
                        DiscordChannel channel = await client.GetChannelAsync(ticket.channelID);
                        if (channel?.GuildId == e.Guild.Id)
                        {
                            await logChannel.SendMessageAsync(new DiscordEmbedBuilder
                            {
                                Color = DiscordColor.Red,
                                Description = "Assigned staff member '" + e.Member.Username + "#" + e.Member.Discriminator + "' has left the server: <#" + channel.Id + ">"
                            });
                        }
                    }
                    catch (Exception) { /* ignored */ }
                }
            }
        }
    }

    public static async Task OnComponentInteractionCreated(DiscordClient client, ComponentInteractionCreatedEventArgs e)
    {
        try
        {
            switch (e.Interaction.Data.ComponentType)
            {
                case DiscordComponentType.Button:
                    switch (e.Id)
                    {
                        case "supportboi_closeconfirm":
                            await CloseCommand.OnConfirmed(e.Interaction);
                            return;
                        case not null when e.Id.StartsWith("supportboi_newcommandbutton"):
                            await NewCommand.OnCategorySelection(e.Interaction);
                            return;
                        case not null when e.Id.StartsWith("supportboi_newticketbutton"):
                            await CreateButtonPanelCommand.OnButtonUsed(e.Interaction);
                            return;
                        case "right":
                            return;
                        case "left":
                            return;
                        case "rightskip":
                            return;
                        case "leftskip":
                            return;
                        case "stop":
                            return;
                        default:
                            Logger.Warn("Unknown button press received! '" + e.Id + "'");
                            return;
                    }
                case DiscordComponentType.StringSelect:
                    switch (e.Id)
                    {
                        case not null when e.Id.StartsWith("supportboi_newcommandselector"):
                            await NewCommand.OnCategorySelection(e.Interaction);
                            return;
                        case not null when e.Id.StartsWith("supportboi_newticketselector"):
                            await CreateSelectionBoxPanelCommand.OnSelectionMenuUsed(e.Interaction);
                            return;
                        default:
                            Logger.Warn("Unknown selection box option received! '" + e.Id + "'");
                            return;
                    }
                case DiscordComponentType.ActionRow:
                    Logger.Warn("Unknown action row received! '" + e.Id + "'");
                    return;
                case DiscordComponentType.FormInput:
                    Logger.Warn("Unknown form input received! '" + e.Id + "'");
                    return;
                default:
                    Logger.Warn("Unknown interaction type received! '" + e.Interaction.Data.ComponentType + "'");
                    break;
            }
        }
        catch (DiscordException ex)
        {
            Logger.Error("Interaction Exception occurred: " + ex);
            Logger.Error("JsomMessage: " + ex.JsonMessage);
        }
        catch (Exception ex)
        {
            Logger.Error("Interaction Exception occured: " + ex.GetType() + ": " + ex);
        }
    }
    public static async Task OnCommandError(CommandsExtension commandSystem, CommandErroredEventArgs e)
    {
        switch (e.Exception)
        {
            case ChecksFailedException checksFailedException:
            {
                foreach (ContextCheckFailedData error in checksFailedException.Errors)
                {
                    await e.Context.Channel.SendMessageAsync(new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Red,
                        Description = error.ErrorMessage
                    });
                }
                return;
            }

            case BadRequestException ex:
                Logger.Error("Command exception occured:\n" + e.Exception);
                Logger.Error("JSON Message: " + ex.JsonMessage);
                return;

            default:
            {
                Logger.Error("Exception occured: " + e.Exception.GetType() + ": " + e.Exception);
                await e.Context.Channel.SendMessageAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "Internal error occured, please report this to the developer."
                });
                return;
            }
        }
    }

}