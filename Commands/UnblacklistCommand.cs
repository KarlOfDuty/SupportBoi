using System;
using System.ComponentModel;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

namespace SupportBoi.Commands;

public class UnblacklistCommand
{
    [RequireGuild]
    [Command("unblacklist")]
    [Description("Unblacklists a user from opening tickets.")]
    public async Task OnExecute(SlashCommandContext command, [Parameter("user")] [Description("User to remove from blacklist.")] DiscordUser user)
    {
        try
        {
            if (!Database.Unblacklist(user.Id))
            {
                await command.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = user.Mention + " is not blacklisted."
                }, true);
                return;
            }

            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "Unblocked " + user.Mention + " from opening new tickets."
            }, true);

            await LogChannel.Success(user.Mention + " was unblocked from opening tickets by " + command.User.Mention + ".");
        }
        catch (Exception)
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Error occured while removing " + user.Mention + " from blacklist."
            }, true);
            throw;
        }
    }
}