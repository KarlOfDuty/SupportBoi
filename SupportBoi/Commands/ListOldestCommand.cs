﻿using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace SupportBoi.Commands
{
	public class ListOldestCommand : BaseCommandModule
	{
		[Command("listoldest")]
		[Aliases("lo")]
		[Cooldown(1, 5, CooldownBucketType.User)]
		public async Task OnExecute(CommandContext command, [RemainingText] string commandArgs)
		{
			// Check if the user has permission to use this command.
			if (!Config.HasPermission(command.Member, "listoldest"))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "You do not have permission to use this command."
				};
				await command.RespondAsync(error);
				command.Client.Logger.Log(LogLevel.Information, "User tried to use the listoldest command but did not have permission.");
				return;
			}

			int listLimit = 20;
			if (!string.IsNullOrEmpty(command.RawArgumentString?.Trim() ?? ""))
			{
				if (!int.TryParse(command.RawArgumentString?.Trim(), out listLimit) || listLimit < 5 || listLimit > 100)
				{
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "Invalid list amount. (Must be integer between 5 and 100)"
					};
					await command.RespondAsync(error);
					return;
				}
			}

			if (!Database.TryGetOldestTickets(command.Member.Id, out List<Database.Ticket> openTickets, listLimit))
			{
				DiscordEmbed channelInfo = new DiscordEmbedBuilder()
					.WithColor(DiscordColor.Red)
					.WithDescription("Could not fetch any open tickets.");
				await command.RespondAsync(channelInfo);
				return;
			}

			List<string> listItems = new List<string>();
			foreach (Database.Ticket ticket in openTickets)
			{
				listItems.Add("**" + ticket.FormattedCreatedTime() + ":** <#" + ticket.channelID + "> by <@" + ticket.creatorID + ">\n");
			}

			LinkedList<string> messages = Utilities.ParseListIntoMessages(listItems);
			foreach (string message in messages)
			{
				DiscordEmbed channelInfo = new DiscordEmbedBuilder()
					.WithTitle("The " + openTickets.Count + " oldest open tickets: ")
					.WithColor(DiscordColor.Green)
					.WithDescription(message?.Trim());
				await command.RespondAsync(channelInfo);
			}
		}
	}
}
