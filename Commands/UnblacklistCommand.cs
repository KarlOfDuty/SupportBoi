using System;
using System.ComponentModel;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;

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
                Description = "Removed " + user.Mention + " from blacklist."
            }, true);

            // TODO: This throws an exception instead of returning null now
            // Log it if the log channel exists
            DiscordChannel logChannel = await command.Guild.GetChannelAsync(Config.logChannel);
            if (logChannel != null)
            {
                await logChannel.SendMessageAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Green,
                    Description = user.Mention + " was unblacklisted from opening tickets by " + command.Member.Mention + "."
                });
            }
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