using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SupportBoi.Commands
{
	public class BlacklistCommand : ApplicationCommandModule
	{
		[SlashRequireGuild]
		[SlashCommand("blacklist", "Blacklists a user from opening tickets.")]
		public async Task OnExecute(InteractionContext command, [Option("User", "User to blacklist.")] DiscordUser user)
		{
			try
			{
				if (!Database.Blacklist(user.Id, command.User.Id))
				{
					await command.CreateResponseAsync(new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = user.Mention + " is already blacklisted."
					}, true);
					return;
				}

				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = "Blacklisted " + user.Mention + "."
				}, true);

				// Log it if the log channel exists
				DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
				if (logChannel != null)
				{
					await logChannel.SendMessageAsync(new DiscordEmbedBuilder
					{
						Color = DiscordColor.Green,
						Description = user.Mention + " was blacklisted from opening tickets by " + command.Member.Mention + "."
					});
				}
			}
			catch (Exception)
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error occured while blacklisting " + user.Mention + "."
				}, true);
				throw;
			}
		}
	}
}
