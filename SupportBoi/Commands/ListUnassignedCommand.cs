using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace SupportBoi.Commands
{
	public class ListUnassignedCommand
	{
		[Command("listunassigned")]
		[Aliases("lu")]
		[Cooldown(1, 5, CooldownBucketType.User)]
		public async Task OnExecute(CommandContext command)
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
				command.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi", "User tried to use the listunassigned command but did not have permission.", DateTime.Now);
				return;
			}

			if (!Database.TryGetAssignedTickets(0, out List<Database.Ticket> unassignedTickets))
			{
				DiscordEmbed response = new DiscordEmbedBuilder()
					.WithColor(DiscordColor.Green)
					.WithDescription("There are no unassigned tickets.");
				await command.RespondAsync("", false, response);
			}

			List<string> listItems = new List<string>();
			foreach (Database.Ticket ticket in unassignedTickets)
			{
				listItems.Add("**" + ticket.FormattedCreatedTime() + ":** <#" + ticket.channelID + "> by <@" + ticket.creatorID + ">\n");
			}

			LinkedList<string> messages = Utilities.ParseListIntoMessages(listItems);
			foreach (string message in messages)
			{
				DiscordEmbed channelInfo = new DiscordEmbedBuilder()
					.WithTitle("Unassigned tickets: ")
					.WithColor(DiscordColor.Green)
					.WithDescription(message?.Trim());
				await command.RespondAsync("", false, channelInfo);
			}
		}
	}
}
