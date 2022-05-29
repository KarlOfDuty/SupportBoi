using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace SupportBoi.Commands
{
	public class UnassignCommand : BaseCommandModule
	{
		[Command("unassign")]
		[Description("Unassigns a staff member from a ticket.")]
		public async Task OnExecute(CommandContext command, [RemainingText] string commandArgs)
		{
			if (!await Utilities.VerifyPermission(command, "unassign")) return;

			// Check if ticket exists in the database
			if (!Database.TryGetOpenTicket(command.Channel.Id, out Database.Ticket ticket))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "This channel is not a ticket."
				};
				await command.RespondAsync(error);
				return;
			}

			if (!Database.UnassignStaff(ticket))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error: Failed to unassign staff from ticket."
				};
				await command.RespondAsync(error);
				return;
			}

			DiscordEmbed message = new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Unassigned staff from ticket."
			};
			await command.RespondAsync(message);

			// Log it if the log channel exists
			DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
			if (logChannel != null)
			{
				DiscordEmbed logMessage = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = "Staff was unassigned from " + command.Channel.Mention + " by " + command.Member.Mention + "."
				};
				await logChannel.SendMessageAsync(logMessage);
			}
		}
	}
}
