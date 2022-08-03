using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Microsoft.Extensions.Logging;

namespace SupportBoi.Commands
{
	public class ListAssignedCommand : ApplicationCommandModule
	{
		[SlashRequireGuild]
		[Config.ConfigPermissionCheckAttribute("listassigned")]
		[SlashCommand("listassigned", "Lists tickets assigned to a user.")]
		public async Task OnExecute(InteractionContext command, DiscordUser user = null)
		{
			DiscordUser listUser = user == null ? command.User : user;
			
			if (!Database.TryGetAssignedTickets(listUser.Id, out List<Database.Ticket> assignedTickets))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "User does not have any assigned tickets."
				});
				return;
			}

			List<string> listItems = new List<string>();
			foreach (Database.Ticket ticket in assignedTickets)
			{
				listItems.Add("**" + ticket.FormattedCreatedTime() + ":** <#" + ticket.channelID + "> by <@" + ticket.creatorID + ">\n");
			}

			bool replySent = false;
			LinkedList<string> messages = Utilities.ParseListIntoMessages(listItems);
			foreach (string message in messages)
			{
				DiscordEmbed channelInfo = new DiscordEmbedBuilder()
					.WithTitle("Assigned tickets: ")
					.WithColor(DiscordColor.Green)
					.WithDescription(message);
				
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
