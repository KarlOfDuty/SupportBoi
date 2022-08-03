using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SupportBoi.Commands
{
	public class ListUnassignedCommand : ApplicationCommandModule
	{
		[SlashRequireGuild]
		[Config.ConfigPermissionCheckAttribute("listunassigned")]
		[SlashCommand("listunassigned", "Lists unassigned tickets.")]
		public async Task OnExecute(InteractionContext command, int limit = 20)
		{
			if (!Database.TryGetAssignedTickets(0, out List<Database.Ticket> unassignedTickets))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder()
				{
					Color = DiscordColor.Green,
					Description = "There are no unassigned tickets."
				});
			}

			List<string> listItems = new List<string>();
			foreach (Database.Ticket ticket in unassignedTickets)
			{
				listItems.Add("**" + ticket.FormattedCreatedTime() + ":** <#" + ticket.channelID + "> by <@" + ticket.creatorID + ">\n");
			}

			bool replySent = false;
			LinkedList<string> messages = Utilities.ParseListIntoMessages(listItems);
			foreach (string message in messages)
			{
				DiscordEmbed channelInfo = new DiscordEmbedBuilder()
				{
					Title = "Unassigned tickets: ",
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
