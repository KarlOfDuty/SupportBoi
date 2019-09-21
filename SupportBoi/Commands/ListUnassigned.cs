using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MySql.Data.MySqlClient;

namespace SupportBoi.Commands
{
	public class ListUnassigned
	{
		[Command("listunassigned")]
		[Cooldown(1, 5, CooldownBucketType.User)]
		public async Task OnExecute(CommandContext command)
		{
			using (MySqlConnection c = Database.GetConnection())
			{
				// Check if the user has permission to use this command.
				if (!Config.HasPermission(command.Member, "listunassigned"))
				{
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "You do not have permission to use this command."
					};
					await command.RespondAsync("", false, error);
					command.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi", "User tried to use the listunassigned command but did not have permission.", DateTime.UtcNow);
					return;
				}

				if (Database.TryGetAssignedTickets(0, out List<Database.Ticket> unassignedTickets))
				{
					DiscordEmbed channelInfo = new DiscordEmbedBuilder()
						.WithTitle("Unassigned tickets: ")
						.WithColor(DiscordColor.Green)
						.WithDescription(string.Join(", ", unassignedTickets.Select(x => "<#" + x.channelID + ">")));
					await command.RespondAsync("", false, channelInfo);
				}
				else
				{
					DiscordEmbed channelInfo = new DiscordEmbedBuilder()
						.WithColor(DiscordColor.Red)
						.WithDescription("There are no unassigned tickets.");
					await command.RespondAsync("", false, channelInfo);
				}
			}
		}
	}
}
