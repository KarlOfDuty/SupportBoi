using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace SupportBoi.Commands
{
	public class SummaryCommand : BaseCommandModule
	{
		[Command("summary")]
		[Cooldown(1, 5, CooldownBucketType.User)]
		public async Task OnExecute(CommandContext command, [RemainingText] string commandArgs)
		{
			if (!await Utilities.VerifyPermission(command, "summary")) return;

			if (Database.TryGetOpenTicket(command.Channel.Id, out Database.Ticket ticket))
			{
				DiscordEmbed channelInfo = new DiscordEmbedBuilder()
					.WithTitle("Channel information")
					.WithColor(DiscordColor.Cyan)
					.AddField("Ticket number:", ticket.id.ToString(), true)
					.AddField("Ticket creator:", $"<@{ticket.creatorID}>", true)
					.AddField("Assigned staff:", ticket.assignedStaffID == 0 ? "Unassigned." : $"<@{ticket.assignedStaffID}>", true)
					.AddField("Creation time:", ticket.createdTime.ToString(Config.timestampFormat), true)
					.AddField("Summary:", string.IsNullOrEmpty(ticket.summary) ? "No summary." : ticket.summary, false);
				await command.RespondAsync(channelInfo);
			}
			else
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "This channel is not a ticket."
				};
				await command.RespondAsync(error);
			}
		}
	}
}
