﻿using System;
using System.ComponentModel;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

namespace SupportBoi.Commands;

public class AssignCommand
{
    [RequireGuild]
    [Command("assign")]
    [Description("Assigns a staff member to this ticket.")]
    public async Task OnExecute(SlashCommandContext command,
        [Parameter("user")] [Description("(Optional) User to assign to this ticket.")] DiscordUser user = null)
    {
        DiscordMember member = null;
        try
        {
            member = user == null ? command.Member : await command.Guild.GetMemberAsync(user.Id);

            if (member == null)
            {
                await command.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "Could not find that user in this server."
                }, true);
                return;
            }
        }
        catch (Exception)
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Could not find that user in this server."
            }, true);
            return;
        }

        // Check if ticket exists in the database
        if (!Database.Ticket.TryGetOpenTicket(command.Channel.Id, out Database.Ticket ticket))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "This channel is not a ticket."
            }, true);
            return;
        }

        if (!Database.StaffMember.IsStaff(member.Id))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Error: User is not registered as staff."
            }, true);
            return;
        }

        if (!Database.StaffMember.AssignStaff(ticket, member.Id))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Error: Failed to assign " + member.Mention + " to ticket."
            }, true);
            return;
        }

        await command.RespondAsync(new DiscordEmbedBuilder
        {
            Color = DiscordColor.Green,
            Description = "Assigned " + member.Mention + " to ticket."
        });

        if (Config.assignmentNotifications)
        {
            try
            {
                await member.SendMessageAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Green,
                    Description = "You have been assigned to a support ticket: " + command.Channel.Mention
                });
            }
            catch (UnauthorizedException) { /* ignore */ }
        }

        await LogChannel.Success(member.Mention + " was assigned to " + command.Channel.Mention + " by " + command.User.Mention + ".", ticket.id);
    }
}