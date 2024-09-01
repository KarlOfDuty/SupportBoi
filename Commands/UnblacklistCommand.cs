using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SupportBoi.Commands;

public class UnblacklistCommand : ApplicationCommandModule
{
    [SlashRequireGuild]
    [SlashCommand("unblacklist", "Unblacklists a user from opening tickets.")]
    public async Task OnExecute(InteractionContext command, [Option("User", "User to remove from blacklist.")] DiscordUser user)
    {
        try
        {
            if (!Database.Unblacklist(user.Id))
            {
                await command.CreateResponseAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = user.Mention + " is not blacklisted."
                }, true);
                return;
            }

            await command.CreateResponseAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "Removed " + user.Mention + " from blacklist."
            }, true);

            // Log it if the log channel exists
            DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
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
            await command.CreateResponseAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Error occured while removing " + user.Mention + " from blacklist."
            }, true);
            throw;
        }
    }
}