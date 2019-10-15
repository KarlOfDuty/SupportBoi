using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace SupportBoi.Commands
{
	public class ListCommand
	{
		[Command("list")]
		[Cooldown(1, 5, CooldownBucketType.User)]
		public async Task OnExecute(CommandContext command)
		{
			// Check if the user has permission to use this command.
			if (!Config.HasPermission(command.Member, "list"))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "You do not have permission to use this command."
				};
				await command.RespondAsync("", false, error);
				command.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi", "User tried to use the list command but did not have permission.", DateTime.UtcNow);
				return;
			}

			ulong userID;
			string strippedMessage = command.Message.Content.Replace(Config.prefix, "");
			string[] parsedMessage = strippedMessage.Replace("<@!", "").Replace("<@", "").Replace(">", "").Split();

			if (parsedMessage.Length < 2)
			{
				userID = command.Member.Id;
			}
			else if (!ulong.TryParse(parsedMessage[1], out userID))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Invalid ID/Mention. (Could not convert to numerical)"
				};
				await command.RespondAsync("", false, error);
				return;
			}

			if (Database.TryGetOpenTickets(userID, out List<Database.Ticket> openTickets))
			{
				DiscordEmbed channelInfo = new DiscordEmbedBuilder()
					.WithTitle("Open tickets: ")
					.WithColor(DiscordColor.Green)
					.WithDescription(string.Join(", ", openTickets.Select(x => "<#" + x.channelID + ">")));
				await command.RespondAsync("", false, channelInfo);
			}
			else
			{
				DiscordEmbed channelInfo = new DiscordEmbedBuilder()
					.WithColor(DiscordColor.Green)
					.WithDescription("User does not have any open tickets.");
				await command.RespondAsync("", false, channelInfo);
			}

			if (Database.TryGetClosedTickets(userID, out List<Database.Ticket> closedTickets))
			{
				DiscordEmbed channelInfo = new DiscordEmbedBuilder()
					.WithTitle("Closed tickets: ")
					.WithColor(DiscordColor.Red)
					.WithDescription(string.Join(", ", closedTickets.Select(x => x.id.ToString("00000"))));
				await command.RespondAsync("", false, channelInfo);
			}
			else
			{
				DiscordEmbed channelInfo = new DiscordEmbedBuilder()
					.WithColor(DiscordColor.Red)
					.WithDescription("User does not have any closed tickets.");
				await command.RespondAsync("", false, channelInfo);
			}
		}
	}
}
