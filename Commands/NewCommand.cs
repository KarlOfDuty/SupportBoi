using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using SupportBoi.Interviews;

namespace SupportBoi.Commands;
public class NewCommand
{
    [RequireGuild]
    [Command("new")]
    [Description("Opens a new ticket.")]
    public async Task OnExecute(SlashCommandContext command)
    {
        List<Database.Category> verifiedCategories = await Utilities.GetVerifiedCategories();
        switch (verifiedCategories.Count)
        {
            case 0:
                await command.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "Error: No registered categories found."
                }, true);
                return;
            case 1:
                await command.DeferResponseAsync(true);
                (bool success, string message) = await OpenNewTicket(command.User.Id, command.Channel.Id, verifiedCategories[0].id);

                if (success)
                {
                    await command.FollowupAsync(new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Green,
                        Description = message
                    }, true);
                }
                else
                {
                    await command.FollowupAsync(new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Red,
                        Description = message
                    }, true);
                }
                return;
            default:
                if (Config.newCommandUsesSelector)
                {
                    await CreateSelector(command, verifiedCategories);
                }
                else
                {
                    await CreateButtons(command, verifiedCategories);
                }
                return;
        }
    }

    public static async Task CreateButtons(SlashCommandContext command, List<Database.Category> verifiedCategories)
    {
        DiscordInteractionResponseBuilder builder = new DiscordInteractionResponseBuilder().WithContent(" ");

        int nrOfButtons = 0;
        for (int nrOfButtonRows = 0; nrOfButtonRows < 5 && nrOfButtons < verifiedCategories.Count; nrOfButtonRows++)
        {
            List<DiscordButtonComponent> buttonRow = new List<DiscordButtonComponent>();

            for (; nrOfButtons < 5 * (nrOfButtonRows + 1) && nrOfButtons < verifiedCategories.Count; nrOfButtons++)
            {
                buttonRow.Add(new DiscordButtonComponent(DiscordButtonStyle.Primary, "supportboi_newcommandbutton " + verifiedCategories[nrOfButtons].id, verifiedCategories[nrOfButtons].name));
            }
            builder.AddComponents(buttonRow);
        }

        await command.RespondAsync(builder.AsEphemeral());
    }

    public static async Task CreateSelector(SlashCommandContext command, List<Database.Category> verifiedCategories)
    {
        verifiedCategories = verifiedCategories.OrderBy(x => x.name).ToList();
        List<DiscordSelectComponent> selectionComponents = new List<DiscordSelectComponent>();

        int selectionOptions = 0;
        for (int selectionBoxes = 0; selectionBoxes < 5 && selectionOptions < verifiedCategories.Count; selectionBoxes++)
        {
            List<DiscordSelectComponentOption> categoryOptions = new List<DiscordSelectComponentOption>();

            for (; selectionOptions < 25 * (selectionBoxes + 1) && selectionOptions < verifiedCategories.Count; selectionOptions++)
            {
                categoryOptions.Add(new DiscordSelectComponentOption(verifiedCategories[selectionOptions].name, verifiedCategories[selectionOptions].id.ToString()));
            }
            selectionComponents.Add(new DiscordSelectComponent("supportboi_newcommandselector" + selectionBoxes, "Open new ticket...", categoryOptions, false, 0, 1));
        }

        await command.RespondAsync(new DiscordInteractionResponseBuilder().AddComponents(selectionComponents).AsEphemeral());
    }

    public static async Task<(bool, string)> OpenNewTicket(ulong userID, ulong commandChannelID, ulong categoryID)
    {
        // Check if user is blacklisted
        if (Database.IsBlacklisted(userID))
        {
            return (false, "You are banned from opening tickets.");
        }

        if (Database.IsOpenTicket(commandChannelID))
        {
            return (false, "You cannot use this command in a ticket channel.");
        }

        if (!Database.IsStaff(userID)
          && Database.TryGetOpenTickets(userID, out List<Database.Ticket> ownTickets)
          && (ownTickets.Count >= Config.ticketLimit && Config.ticketLimit != 0))
        {
            return (false, "You have reached the limit for maximum open tickets.");
        }

        DiscordChannel category = null;
        try
        {
            category = await SupportBoi.client.GetChannelAsync(categoryID);
        }
        catch (Exception) { /*ignored*/ }

        if (category == null)
        {
            return (false, "Error: Could not find the category to place the ticket in.");
        }

        DiscordMember member = null;
        try
        {
            member = await category.Guild.GetMemberAsync(userID);
        }
        catch (Exception) { /*ignored*/ }

        if (member == null)
        {
            return (false, "Error: Could not find you on the Discord server.");
        }

        DiscordChannel ticketChannel = null;

        try
        {
            ticketChannel = await category.Guild.CreateChannelAsync("ticket", DiscordChannelType.Text, category);
        }
        catch (Exception) { /* ignored */ }

        if (ticketChannel == null)
        {
            return (false, "Error occured while creating ticket, " + member.Mention +
                           "!\nIs the channel limit reached in the server or ticket category?");
        }

        ulong staffID = 0;
        if (Config.randomAssignment)
        {
            staffID = Database.GetRandomActiveStaff(0)?.userID ?? 0;
        }

        long id = Database.NewTicket(member.Id, staffID, ticketChannel.Id);
        try
        {
            await ticketChannel.ModifyAsync(modifiedAttributes => modifiedAttributes.Name = "ticket-" + id.ToString("00000"));
        }
        catch (DiscordException e)
        {
            Logger.Error("Exception occurred trying to modify channel: " + e);
            Logger.Error("JsomMessage: " + e.JsonMessage);
        }

        try
        {
            await ticketChannel.AddOverwriteAsync(member, DiscordPermissions.AccessChannels);
        }
        catch (DiscordException e)
        {
            Logger.Error("Exception occurred trying to add channel permissions: " + e);
            Logger.Error("JsomMessage: " + e.JsonMessage);
        }

        DiscordMessage message = await ticketChannel.SendMessageAsync("Hello, " + member.Mention + "!\n" + Config.welcomeMessage);

        if (Config.pinFirstMessage)
        {
            try
            {
                await message.PinAsync();
            }
            catch (Exception e)
            {
                Logger.Error("Exception occurred trying to pin message: ", e);
            }
        }

        // Refreshes the channel as changes were made to it above
        ticketChannel = await SupportBoi.client.GetChannelAsync(ticketChannel.Id);

        if (Config.interviewsEnabled)
        {
            Interviewer.StartInterview(ticketChannel);
        }

        if (staffID != 0)
        {
            await ticketChannel.SendMessageAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "Ticket was randomly assigned to <@" + staffID + ">."
            });

            if (Config.assignmentNotifications)
            {
                try
                {
                    DiscordMember staffMember = await category.Guild.GetMemberAsync(staffID);
                    await staffMember.SendMessageAsync(new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Green,
                        Description = "You have been randomly assigned to a newly opened support ticket: " +
                                      ticketChannel.Mention
                    });
                }
                catch (DiscordException e)
                {
                    Logger.Error("Exception occurred assign random staff member: " + e);
                    Logger.Error("JsomMessage: " + e.JsonMessage);
                }
            }
        }

        try
        {
            // Log it if the log channel exists
            DiscordChannel logChannel = await SupportBoi.client.GetChannelAsync(Config.logChannel);
            await logChannel.SendMessageAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "Ticket " + ticketChannel.Mention + " opened by " + member.Mention + ".",
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

        return (true, "Ticket opened, " + member.Mention + "!\n" + ticketChannel.Mention);
    }
}
