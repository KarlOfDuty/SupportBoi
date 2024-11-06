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
            if (!Database.Blacklist(user.Id, command.User.Id))
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

            try
            {
                // Log it if the log channel exists
                DiscordChannel logChannel = await SupportBoi.client.GetChannelAsync(Config.logChannel);
                await logChannel.SendMessageAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Green,
                    Description = user.Mention + " was blocked from opening tickets by " + command.User.Mention + "."
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
                Description = "Error occured while blacklisting " + user.Mention + "."
            }, true);
            throw;
        }
    }
}