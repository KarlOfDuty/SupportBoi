using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SupportBoi.Commands
{
	public class AssignCommand : ApplicationCommandModule
	{
		[SlashRequireGuild]
		[Config.ConfigPermissionCheckAttribute("assign")]
		[SlashCommand("assign", "Assigns a staff member to this ticket.")]
		public async Task OnExecute(InteractionContext command, [Option("User", "User to assign to this ticket.")] DiscordUser user)
		{
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
			
			// Check if ticket exists in the database
			if (!Database.TryGetOpenTicket(command.Channel.Id, out Database.Ticket ticket))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "This channel is not a ticket."
				}, true);
				return;
			}

			if (!Database.IsStaff(member.Id))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error: User is not registered as staff."
				}, true);
				return;
			}

			if (!Database.AssignStaff(ticket, member.Id))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error: Failed to assign " + member.Mention + " to ticket."
				}, true);
				return;
			}

			await command.CreateResponseAsync(new DiscordEmbedBuilder
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
				catch (UnauthorizedException) {}
			}

			// Log it if the log channel exists
			DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
			if (logChannel != null)
			{
				await logChannel.SendMessageAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = member.Mention + " was assigned to " + command.Channel.Mention + " by " + command.Member.Mention + "."
				});
			}
		}
	}
}
