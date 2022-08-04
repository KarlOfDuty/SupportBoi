using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SupportBoi.Commands
{
	public class ListOldestCommand : ApplicationCommandModule
	{
		[SlashRequireGuild]
		[Config.ConfigPermissionCheckAttribute("listoldest")]
		[SlashCommand("listoldest", "Lists the oldest open tickets.")]
		public async Task OnExecute(InteractionContext command, [Option("Limit", "(Optional) Limit of how many tickets to list.")] long limit = 20)
		{
			int clampedLimit = Math.Clamp((int)limit, 1, 200);
			
			if (!Database.TryGetOldestTickets(command.Member.Id, out List<Database.Ticket> openTickets, clampedLimit))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder()
				{
					Color = DiscordColor.Red,
					Description = "Could not fetch any open tickets."
				});
				return;
			}

			List<string> listItems = new List<string>();
			foreach (Database.Ticket ticket in openTickets)
			{
				listItems.Add("**" + ticket.FormattedCreatedTime() + ":** <#" + ticket.channelID + "> by <@" + ticket.creatorID + ">\n");
			}

			bool replySent = false;
			LinkedList<string> messages = Utilities.ParseListIntoMessages(listItems);
			foreach (string message in messages)
			{
				DiscordEmbed channelInfo = new DiscordEmbedBuilder()
				{
					Title = "The " + openTickets.Count + " oldest open tickets: ",
					Color = DiscordColor.Green,
					Description = message?.Trim()
				};
				
				// We have to send exactly one reply to the interaction and all other messages as normal messages
				if (replySent)
				{
					await command.Channel.SendMessageAsync(channelInfo);
				}
				else
				{
					await command.CreateResponseAsync(channelInfo);
					replySent = true;
				}
			}
		}
	}
}
