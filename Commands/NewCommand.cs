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
        DiscordInteractionResponseBuilder builder = new();

        int nrOfButtons = 0;
        for (int nrOfButtonRows = 0; nrOfButtonRows < 5 && nrOfButtons < verifiedCategories.Count; nrOfButtonRows++)
        {
            List<DiscordButtonComponent> buttonRow = [];

            for (; nrOfButtons < 5 * (nrOfButtonRows + 1) && nrOfButtons < verifiedCategories.Count; nrOfButtons++)
            {
                buttonRow.Add(new DiscordButtonComponent(DiscordButtonStyle.Primary, "supportboi_newcommandbutton " + verifiedCategories[nrOfButtons].id, verifiedCategories[nrOfButtons].name));
            }
            builder.AddActionRowComponent(new DiscordActionRowComponent(buttonRow));
        }

        await command.RespondAsync(builder.AsEphemeral());
    }

    public static async Task CreateSelector(SlashCommandContext command, List<Database.Category> verifiedCategories)
    {
        DiscordInteractionResponseBuilder builder = new();
        verifiedCategories = verifiedCategories.OrderBy(x => x.name).ToList();

        int selectionOptions = 0;
        List<DiscordSelectComponent> selectionComponents = [];
        for (int selectionBoxes = 0; selectionBoxes < 5 && selectionOptions < verifiedCategories.Count; selectionBoxes++)
        {
            List<DiscordSelectComponentOption> categoryOptions = [];
            for (; selectionOptions < 25 * (selectionBoxes + 1) && selectionOptions < verifiedCategories.Count; selectionOptions++)
            {
                categoryOptions.Add(new DiscordSelectComponentOption(verifiedCategories[selectionOptions].name, verifiedCategories[selectionOptions].id.ToString()));
            }
            selectionComponents.Add(new DiscordSelectComponent("supportboi_newcommandselector" + selectionBoxes, "Open new ticket...", categoryOptions, false, 0, 1));
        }

        await command.RespondAsync(builder.AddActionRowComponent(new DiscordActionRowComponent(selectionComponents)).AsEphemeral());
    }

    public static async Task<(bool, string)> OpenNewTicket(ulong userID, ulong commandChannelID, ulong categoryID)
    {
        // Check if user is blacklisted
        if (Database.Blacklist.IsBanned(userID))
        {
            return (false, "You are banned from opening tickets.");
        }

        if (Config.globalTicketLimit != 0
          && !Database.StaffMember.IsStaff(userID)
          && Database.Ticket.GetNumberOfTickets() >= Config.globalTicketLimit)
        {
            return (false, "The bot has reached the maximum allowed open tickets, it cannot open more at this time.\n\nPlease try again later.");
        }

        if (Database.Ticket.IsOpenTicket(commandChannelID))
        {
            return (false, "You cannot use this command in a ticket channel.");
        }

        if (Config.userTicketLimit != 0
          && !Database.StaffMember.IsStaff(userID)
          && Database.Ticket.TryGetOpenTickets(userID, out List<Database.Ticket> ownTickets)
          && ownTickets.Count >= Config.userTicketLimit)
        {
            return (false, "You have reached the limit for maximum number of open tickets.\n\nPlease close an existing ticket before opening a new one.");
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

        if (category.Children.Count == 50)
        {
            return (false, "This ticket category is full, can not create ticket channel.");
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
        catch (Exception e)
        {
            Logger.Error("Error occured while creating ticket.", e);
        }

        if (ticketChannel == null)
        {
            return (false, "Error occured while creating ticket, " + member.Mention +
                           "!\nIs the channel limit reached in the server or ticket category?");
        }

        DiscordMember assignedStaff = null;
        if (Config.randomAssignment)
        {
            assignedStaff = await RandomAssignCommand.GetRandomVerifiedStaffMember(ticketChannel, userID, 0, null);
        }

        long id = Database.Ticket.NewTicket(member.Id, assignedStaff?.Id ?? 0, ticketChannel.Id);
        try
        {
            await ticketChannel.ModifyAsync(modifiedAttributes => modifiedAttributes.Name = "ticket-" + id.ToString("00000"));
        }
        catch (DiscordException e)
        {
            Logger.Error("Exception occurred trying to modify channel: " + e);
            Logger.Error("JsonMessage: " + e.JsonMessage);
        }

        try
        {
            await ticketChannel.AddOverwriteAsync(member, DiscordPermission.ViewChannel);
        }
        catch (DiscordException e)
        {
            Logger.Error("Exception occurred trying to add channel permissions: " + e);
            Logger.Error("JsonMessage: " + e.JsonMessage);
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
            await Interviewer.StartInterview(ticketChannel);
        }

        if (assignedStaff != null)
        {
            await ticketChannel.SendMessageAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "Ticket was randomly assigned to " + assignedStaff.Mention + "."
            });

            if (Config.assignmentNotifications)
            {
                try
                {
                    await assignedStaff.SendMessageAsync(new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Green,
                        Description = "You have been randomly assigned to a newly opened support ticket: " +
                                      ticketChannel.Mention
                    });
                }
                catch (DiscordException e)
                {
                    Logger.Error("Exception occurred assign random staff member: " + e);
                    Logger.Error("JsonMessage: " + e.JsonMessage);
                }
            }
        }

        await LogChannel.Success("Ticket " + ticketChannel.Mention + " opened by " + member.Mention + ".", (uint)id);
        await CategorySuffixHandler.ScheduleSuffixUpdate(category.Id);
        return (true, "Ticket opened, " + member.Mention + "!\n" + ticketChannel.Mention);
    }
}
