using System;
using System.ComponentModel;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using MySqlConnector;

namespace SupportBoi.Commands;

public class AddStaffCommand
{
    [RequireGuild]
    [Command("addstaff")]
    [Description("Adds a new staff member.")]
    public async Task OnExecute(SlashCommandContext command,
        [Parameter("user")] [Description("User to add to staff.")] DiscordUser user)
    {
        DiscordMember staffMember;
        try
        {
            staffMember = user == null ? command.Member : await command.Guild.GetMemberAsync(user.Id);

            if (staffMember == null)
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

        bool alreadyStaff = Database.StaffMember.IsStaff(staffMember.Id);
        if (!Database.StaffMember.AddStaff(staffMember.DisplayName, staffMember.Id))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Error: Failed to add staff member to database."
            }, true);
        }

        if (alreadyStaff)
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = staffMember.Mention + " is already a staff member, refreshed username in database."
            }, true);
        }
        else
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = staffMember.Mention + " was added to staff."
            }, true);

            await LogChannel.Success(staffMember.Mention + " was added to staff by " + command.User.Mention + ".");
        }
    }
}