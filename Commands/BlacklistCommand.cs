using System;
using System.ComponentModel;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

namespace SupportBoi.Commands;

public class BlacklistCommand
{
    [RequireGuild]
    [Command("blacklist")]
    [Description("Blacklists a user from opening tickets.")]
    public async Task OnExecute(SlashCommandContext command,
        [Parameter("user")] [Description("User to blacklist.")] DiscordUser user)
    {
        try
        {
            if (!Database.Blacklist.Ban(user.Id, command.User.Id))
            {
                await command.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = user.Mention + " is already blacklisted."
                }, true);
                return;
            }

            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "Blocked " + user.Mention + " from opening new tickets."
            }, true);

            await LogChannel.Success(user.Mention + " was blocked from opening tickets by " + command.User.Mention + ".");
        }
        catch (Exception)
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Error occured while blacklisting " + user.Mention + "."
            }, true);
            throw;
        }
    }
}