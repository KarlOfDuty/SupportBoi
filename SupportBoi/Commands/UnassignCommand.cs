using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

namespace SupportBoi.Commands
{
	public class UnassignCommand
	{
		[Command("unassign")]
		[Description("Unassigns a staff member from a ticket.")]
		public async Task OnExecute(CommandContext command)
		{
			// Check if the user has permission to use this command.
			if (!Config.HasPermission(command.Member, "unassign"))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "You do not have permission to use this command."
				};
				await command.RespondAsync("", false, error);
				command.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi", "User tried to use the unassign command but did not have permission.", DateTime.UtcNow);
				return;
			}

			// Check if ticket exists in the database
			if (!Database.TryGetTicket(command.Channel.Id, out Database.Ticket ticket))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "This channel is not a ticket."
				};
				await command.RespondAsync("", false, error);
				return;
			}

			if (!Database.UnassignStaff(ticket.id))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error: Failed to unassign staff from ticket."
				};
				await command.RespondAsync("", false, error);
				return;
			}

			DiscordEmbed message = new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Unassigned staff from ticket " + ticket.id + "."
			};
			await command.RespondAsync("", false, message);

			// Log it if the log channel exists
			DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
			if (logChannel != null)
			{
				DiscordEmbed logMessage = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = "Staff was unassigned from " + command.Channel.Mention + " by " + command.Member.Mention + "."
				};
				await logChannel.SendMessageAsync("", false, logMessage);
			}

			if (Config.sheetsEnabled)
			{
				DiscordMember user = null;
				try
				{
					user = await command.Guild.GetMemberAsync(ticket.creatorID);
				}
				catch (NotFoundException) { }

				Sheets.DeleteTicketQueued(ticket.id);
				Sheets.AddTicketQueued(user, command.Channel, ticket.id.ToString(), null, null, ticket.createdTime);
			}
		}
	}
}
