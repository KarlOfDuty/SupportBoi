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

				bool isTicket = Database.IsTicket(command.Channel.Id);

				long openTickets = Database.GetNumberOfTickets();
				long closedTickets = Database.GetNumberOfClosedTickets();

				DiscordEmbed message = new DiscordEmbedBuilder()
					.WithTitle("Bot information")
					.WithColor(DiscordColor.Cyan)
					.AddField("Version", SupportBoi.GetVersion(), false)
					.AddField("Repository", "https://github.com/KarlofDuty/SupportBoi", false)
					.AddField("Open tickets", openTickets + "", true)
					.AddField("Closed tickets (1.1.0+ tickets only)", closedTickets + " ", true)
					.AddField("Channel info", isTicket ? "This channel is a ticket. (More info will be added here in a later version)" : "This channel is not a ticket.", false)
					.WithFooter("KarlofDuty", "https://karlofduty.com/img/tardisIcon.jpg");
				await command.RespondAsync("", false, message);
			}
		}
	}
}
