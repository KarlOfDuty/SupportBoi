using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SupportBoi.Commands
{
	public class ListOpen : ApplicationCommandModule
	{
		[SlashRequireGuild]
		[SlashCommand("listopen", "Lists all open tickets, oldest first.")]
		public async Task OnExecute(InteractionContext command)
		{
			if (!Database.TryGetOpenTickets(out List<Database.Ticket> openTickets))
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
				listItems.Add("**" + ticket.DiscordRelativeTime() + ":** <#" + ticket.channelID + "> by <@" + ticket.creatorID + ">\n");
			}

			List<DiscordEmbedBuilder> embeds = new List<DiscordEmbedBuilder>();
			foreach (string message in Utilities.ParseListIntoMessages(listItems))
			{
				embeds.Add(new DiscordEmbedBuilder()
				{
					Color = DiscordColor.Green,
					Description = message
				});
			}
			
			// Add the footers
			for (int i = 0; i < embeds.Count; i++)
			{
				embeds[i].Footer = new DiscordEmbedBuilder.EmbedFooter
				{
					Text = $"Page {i + 1} / {embeds.Count}"
				};
			}
			
			List<Page> listPages = new List<Page>();
			foreach (DiscordEmbedBuilder embed in embeds)
			{
				listPages.Add(new Page("", embed));
			}
			
			await command.Interaction.SendPaginatedResponseAsync(true, command.User, listPages);
		}
	}
}
