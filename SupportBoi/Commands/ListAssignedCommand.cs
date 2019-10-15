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
	public class ListAssignedCommand
	{
		[Command("listassigned")]
		[Cooldown(1, 5, CooldownBucketType.User)]
		public async Task OnExecute(CommandContext command)
		{
			// Check if the user has permission to use this command.
			if (!Config.HasPermission(command.Member, "listassigned"))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "You do not have permission to use this command."
				};
				await command.RespondAsync("", false, error);
				command.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi", "User tried to use the listassigned command but did not have permission.", DateTime.UtcNow);
				return;
			}

			ulong staffID;
			string strippedMessage = command.Message.Content.Replace(Config.prefix, "");
			string[] parsedMessage = strippedMessage.Replace("<@!", "").Replace("<@", "").Replace(">", "").Split();

			if (parsedMessage.Length < 2)
			{
				staffID = command.Member.Id;
			}
			else if (!ulong.TryParse(parsedMessage[1], out staffID))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Invalid ID/Mention. (Could not convert to numerical)"
				};
				await command.RespondAsync("", false, error);
				return;
			}

			if (Database.TryGetAssignedTickets(staffID, out List<Database.Ticket> assignedTickets))
			{
				DiscordEmbed channelInfo = new DiscordEmbedBuilder()
					.WithTitle("Assigned tickets: ")
					.WithColor(DiscordColor.Green)
					.WithDescription(string.Join(", ", assignedTickets.Select(x => "<#" + x.channelID + ">")));
				await command.RespondAsync("", false, channelInfo);
			}
			else
			{
				DiscordEmbed channelInfo = new DiscordEmbedBuilder()
					.WithColor(DiscordColor.Red)
					.WithDescription("User does not have any assigned tickets.");
				await command.RespondAsync("", false, channelInfo);
			}
		}
	}
}
