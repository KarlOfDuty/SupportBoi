using System;
using System.ComponentModel;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

namespace SupportBoi.Commands;

public class AddCommand
{
    [RequireGuild]
    [Command("add")]
    [Description("Adds a user to this ticket.")]
    public async Task OnExecute(SlashCommandContext command,
        [Parameter("user")] [Description("User to add to ticket.")] DiscordUser user)
    {
        // Check if ticket exists in the database
        if (!Database.IsOpenTicket(command.Channel.Id))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "This channel is not a ticket."
            }, true);
            return;
        }

        DiscordMember member;
        try
        {
            member = (user == null ? command.Member : await command.Guild.GetMemberAsync(user.Id));

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

        try
        {
            await command.Channel.AddOverwriteAsync(member, DiscordPermissions.AccessChannels);
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "Added " + member.Mention + " to ticket."
            });

            // Log it if the log channel exists
            try
            {
                DiscordChannel logChannel = await SupportBoi.client.GetChannelAsync(Config.logChannel);
                await logChannel.SendMessageAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Green,
                    Description = member.Mention + " was added to " + command.Channel.Mention +
                                  " by " + command.Member?.Mention + "."
                });
            }
            catch (NotFoundException)
            {
                Logger.Error("Could not find the log channel.");
            }
        }
        catch (Exception)
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Could not add " + member.Mention + " to ticket, unknown error occured."
            }, true);
        }
    }
}