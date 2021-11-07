﻿using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace SupportBoi.Commands
{
	public class ListUnassignedCommand : BaseCommandModule
	{
		[Command("listunassigned")]
		[Aliases("lu")]
		[Cooldown(1, 5, CooldownBucketType.User)]
		public async Task OnExecute(CommandContext command, [RemainingText] string commandArgs)
		{
			// Check if the user has permission to use this command.
			if (!Config.HasPermission(command.Member, "listunassigned"))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "You do not have permission to use this command."
				};
				await command.RespondAsync(error);
				command.Client.Logger.Log(LogLevel.Information, "User tried to use the listunassigned command but did not have permission.");
				return;
			}

			if (!Database.TryGetAssignedTickets(0, out List<Database.Ticket> unassignedTickets))
			{
				DiscordEmbed response = new DiscordEmbedBuilder()
					.WithColor(DiscordColor.Green)
					.WithDescription("There are no unassigned tickets.");
				await command.RespondAsync(response);
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
				await command.RespondAsync(channelInfo);
			}
		}
	}
}
