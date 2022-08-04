using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SupportBoi.Commands
{
	public class AddCommand : ApplicationCommandModule
	{
		[SlashRequireGuild]
		[Config.ConfigPermissionCheckAttribute("add")]
		[SlashCommand("add", "Adds a user to a ticket")]
		public async Task OnExecute(InteractionContext command, [Option("User", "User to add to ticket.")] DiscordUser user)
		{
			// Check if ticket exists in the database
			if (!Database.IsOpenTicket(command.Channel.Id))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "This channel is not a ticket."
				}, true);
				return;
			}

			DiscordMember member = null;
			try
			{
				member = user == null ? command.Member : await command.Guild.GetMemberAsync(user.Id);

				if (member == null)
				{
					await command.CreateResponseAsync(new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "Could not find that user in this server."
					}, true);
					return;
				}
			}
			catch (Exception)
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Could not find that user in this server."
				}, true);
				return;
			}
			
			try
			{
				await command.Channel.AddOverwriteAsync(member, Permissions.AccessChannels, Permissions.None);
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = "Added " + member.Mention + " to ticket."
				});

				// Log it if the log channel exists
				DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
				if (logChannel != null)
				{
					await logChannel.SendMessageAsync(new DiscordEmbedBuilder
					{
						Color = DiscordColor.Green,
						Description = member.Mention + " was added to " + command.Channel.Mention +
									  " by " + command.Member.Mention + "."
					});
				}
			}
			catch (Exception)
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Could not add " + member.Mention + " to ticket, unknown error occured."
				}, true);
			}
		}
	}
}
