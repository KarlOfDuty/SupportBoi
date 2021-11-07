﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace SupportBoi.Commands
{
	public class ListAssignedCommand : BaseCommandModule
	{
		[Command("listassigned")]
		[Aliases("la")]
		[Cooldown(1, 5, CooldownBucketType.User)]
		public async Task OnExecute(CommandContext command, [RemainingText] string commandArgs)
		{
			// Check if the user has permission to use this command.
			if (!Config.HasPermission(command.Member, "listassigned"))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "You do not have permission to use this command."
				};
				await command.RespondAsync(error);
				command.Client.Logger.Log(LogLevel.Information, "User tried to use the listassigned command but did not have permission.");
				return;
			}

			ulong staffID;
			string[] parsedIDs = Utilities.ParseIDs(command.RawArgumentString);

			if (!parsedIDs.Any())
			{
				staffID = command.Member.Id;
			}
			else if (!ulong.TryParse(parsedIDs[0], out staffID))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Invalid ID/Mention. (Could not convert to numerical)"
				};
				await command.RespondAsync(error);
				return;
			}

			if (!Database.TryGetAssignedTickets(staffID, out List<Database.Ticket> assignedTickets))
			{
				DiscordEmbed error = new DiscordEmbedBuilder()
					.WithColor(DiscordColor.Red)
					.WithDescription("User does not have any assigned tickets.");
				await command.RespondAsync(error);
				return;
			}

			List<string> listItems = new List<string>();
			foreach (Database.Ticket ticket in assignedTickets)
			{
				listItems.Add("**" + ticket.FormattedCreatedTime() + ":** <#" + ticket.channelID + "> by <@" + ticket.creatorID + ">\n");
			}

			LinkedList<string> messages = Utilities.ParseListIntoMessages(listItems);
			foreach (string message in messages)
			{
				DiscordEmbed channelInfo = new DiscordEmbedBuilder()
					.WithTitle("Assigned tickets: ")
					.WithColor(DiscordColor.Green)
					.WithDescription(message);
				await command.RespondAsync(channelInfo);
			}

		}
	}
}
