using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using DSharpPlus.SlashCommands.EventArgs;
using SupportBoi.Commands;

namespace SupportBoi;

internal static class EventHandler
{
    internal static Task OnReady(DiscordClient client, ReadyEventArgs e)
    {
        Logger.Log("Client is ready to process events.");

        // Checking activity type
        if (!Enum.TryParse(Config.presenceType, true, out ActivityType activityType))
        {
            Logger.Log("Presence type '" + Config.presenceType + "' invalid, using 'Playing' instead.");
            activityType = ActivityType.Playing;
        }

        client.UpdateStatusAsync(new DiscordActivity(Config.presenceText, activityType), UserStatus.Online);
        return Task.CompletedTask;
    }

    internal static async Task OnGuildAvailable(DiscordClient discordClient, GuildCreateEventArgs e)
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

    internal static Task OnClientError(DiscordClient _, ClientErrorEventArgs e)
    {
        Logger.Error("Client exception occured:\n" + e.Exception);
        switch (e.Exception)
        {
            case BadRequestException ex:
                Logger.Error("JSON Message: " + ex.JsonMessage);
                break;
            default:
                break;
        }
        return Task.CompletedTask;
    }

    internal static async Task OnMessageCreated(DiscordClient client, MessageCreateEventArgs e)
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

        // Sends a DM to the assigned staff member if at least a day has gone by since the last message and the user sending the message isn't staff
        IReadOnlyList<DiscordMessage> messages = await e.Channel.GetMessagesAsync(2);
        if (messages.Count > 1 && messages[1].Timestamp < DateTimeOffset.UtcNow.AddDays(Config.ticketUpdatedNotificationDelay * -1) && !Database.IsStaff(e.Author.Id))
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

    internal static async Task OnCommandError(SlashCommandsExtension commandSystem, SlashCommandErrorEventArgs e)
    {
        switch (e.Exception)
        {
            case SlashExecutionChecksFailedException checksFailedException:
            {
                foreach (SlashCheckBaseAttribute attr in checksFailedException.FailedChecks)
                {
                    await e.Context.Channel.SendMessageAsync(new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Red,
                        Description = ParseFailedCheck(attr)
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

    internal static async Task OnMemberAdded(DiscordClient client, GuildMemberAddEventArgs e)
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
                        await channel.AddOverwriteAsync(e.Member, Permissions.AccessChannels);
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

    internal static async Task OnMemberRemoved(DiscordClient client, GuildMemberRemoveEventArgs e)
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

    internal static async Task OnComponentInteractionCreated(DiscordClient client, ComponentInteractionCreateEventArgs e)
    {
        try
        {
            switch (e.Interaction.Data.ComponentType)
            {
                case ComponentType.Button:
                    switch (e.Id)
                    {
                        case "supportboi_closeconfirm":
                            await CloseCommand.OnConfirmed(e.Interaction);
                            return;
                        case {} when e.Id.StartsWith("supportboi_newcommandbutton"):
                            await NewCommand.OnCategorySelection(e.Interaction);
                            return;
                        case {} when e.Id.StartsWith("supportboi_newticketbutton"):
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
                case ComponentType.StringSelect:
                    switch (e.Id)
                    {
                        case {} when e.Id.StartsWith("supportboi_newcommandselector"):
                            await NewCommand.OnCategorySelection(e.Interaction);
                            return;
                        case {} when e.Id.StartsWith("supportboi_newticketselector"):
                            await CreateSelectionBoxPanelCommand.OnSelectionMenuUsed(e.Interaction);
                            return;
                        default:
                            Logger.Warn("Unknown selection box option received! '" + e.Id + "'");
                            return;
                    }
                case ComponentType.ActionRow:
                    Logger.Warn("Unknown action row received! '" + e.Id + "'");
                    return;
                case ComponentType.FormInput:
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

    private static string ParseFailedCheck(SlashCheckBaseAttribute attr)
    {
        return attr switch
        {
            SlashRequireDirectMessageAttribute => "This command can only be used in direct messages!",
            SlashRequireOwnerAttribute => "Only the server owner can use that command!",
            SlashRequirePermissionsAttribute => "You don't have permission to do that!",
            SlashRequireBotPermissionsAttribute => "The bot doesn't have the required permissions to do that!",
            SlashRequireUserPermissionsAttribute => "You don't have permission to do that!",
            SlashRequireGuildAttribute => "This command has to be used in a Discord server!",
            _ => "Unknown Discord API error occured, please try again later."
        };
    }
}