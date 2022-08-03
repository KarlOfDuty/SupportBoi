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
		[SlashCommand("assign", "Assigns a staff member to a ticket.")]
		public async Task OnExecute(InteractionContext command, DiscordMember user)
		{
			DiscordMember assignUser = user == null ? command.Member : user;
			
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

			if (!Database.IsStaff(assignUser.Id))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error: User is not registered as staff."
				}, true);
				return;
			}

			if (!Database.AssignStaff(ticket, assignUser.Id))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error: Failed to assign " + assignUser.Mention + " to ticket."
				}, true);
				return;
			}

			await command.CreateResponseAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Assigned " + assignUser.Mention + " to ticket."
			});

			if (Config.assignmentNotifications)
			{
				try
				{
					await assignUser.SendMessageAsync(new DiscordEmbedBuilder
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
					Description = assignUser.Mention + " was assigned to " + command.Channel.Mention + " by " + command.Member.Mention + "."
				});
			}
		}
	}
}
