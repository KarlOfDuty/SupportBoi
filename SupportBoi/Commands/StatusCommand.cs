using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MySql.Data.MySqlClient;

namespace SupportBoi.Commands
{
	public class StatusCommand
	{
		[Command("status")]
		[Cooldown(1, 5, CooldownBucketType.User)]
		public async Task OnExecute(CommandContext command)
		{
			using (MySqlConnection c = Database.GetConnection())
			{
				// Check if the user has permission to use this command.
				if (!Config.HasPermission(command.Member, "status"))
				{
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "You do not have permission to use this command."
					};
					await command.RespondAsync("", false, error);
					command.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi", "User tried to use the status command but did not have permission.", DateTime.UtcNow);
					return;
				}

				long openTickets = Database.GetNumberOfTickets();
				long closedTickets = Database.GetNumberOfClosedTickets();

				DiscordEmbed botInfo = new DiscordEmbedBuilder()
					.WithAuthor("SupportBoi", "https://github.com/KarlofDuty/SupportBoi", "https://karlofduty.com/img/tardisIcon.jpg")
					.WithTitle("Bot information")
					.WithColor(DiscordColor.Cyan)
					.AddField("Version:", SupportBoi.GetVersion(), false)
					.AddField("Open tickets:", openTickets + "", true)
					.AddField("Closed tickets (1.1.0+ tickets only):", closedTickets + " ", true);
				await command.RespondAsync("", false, botInfo);

				if (Database.TryGetTicket(command.Channel.Id, out Database.Ticket ticket))
				{
					DiscordEmbed channelInfo = new DiscordEmbedBuilder()
						.WithAuthor("SupportBoi", "https://github.com/KarlofDuty/SupportBoi", "https://karlofduty.com/img/tardisIcon.jpg")
						.WithTitle("Channel information")
						.WithColor(DiscordColor.Cyan)
						.AddField("Channel info", "This channel is a ticket.", false)
						.AddField("Ticket number:", ticket.id.ToString(), true)
						.AddField("Ticket creator:", $"<@{ticket.creatorID}>", true)
						.AddField("Assigned staff:", ticket.assignedStaffID == 0 ? "Unassigned." : $"<@{ticket.assignedStaffID}>", true)
						.AddField("Creation time:", ticket.createdTime.ToString(Config.timestampFormat), true)
						.AddField("Summary:", string.IsNullOrEmpty(ticket.summary) ? "No summary." : ticket.summary, false);
					await command.RespondAsync("", false, channelInfo);
				}
			}
		}
	}
}
