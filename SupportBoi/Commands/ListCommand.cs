using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SupportBoi.Commands
{
	public class ListCommand : ApplicationCommandModule
	{
		[SlashRequireGuild]
		[Config.ConfigPermissionCheckAttribute("list")]
		[SlashCommand("list", "Lists tickets opened by a user.")]
		public async Task OnExecute(InteractionContext command, DiscordUser user = null)
		{
			bool replySent = false;
			DiscordUser listUser = user == null ? command.User : user;
			if (Database.TryGetOpenTickets(listUser.Id, out List<Database.Ticket> openTickets))
			{
				List<string> listItems = new List<string>();
				foreach (Database.Ticket ticket in openTickets)
				{
					listItems.Add("**" + ticket.FormattedCreatedTime() + ":** <#" + ticket.channelID + ">\n");
				}

				LinkedList<string> messages = Utilities.ParseListIntoMessages(listItems);
				foreach (string message in messages)
				{
					DiscordEmbed channelInfo = new DiscordEmbedBuilder()
					{
						Title = "Open tickets: ",
						Color = DiscordColor.Green,
						Description = message
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
			else
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder()
				{
					Color = DiscordColor.Green,
					Description = "User does not have any open tickets."
				});
			}

			if (Database.TryGetClosedTickets(listUser.Id, out List<Database.Ticket> closedTickets))
			{
				List<string> listItems = new List<string>();
				foreach (Database.Ticket ticket in closedTickets)
				{
					listItems.Add("**" + ticket.FormattedCreatedTime() + ":** Ticket " + ticket.id.ToString("00000") + "\n");
				}

				LinkedList<string> messages = Utilities.ParseListIntoMessages(listItems);
				foreach (string message in messages)
				{
					await command.Channel.SendMessageAsync(new DiscordEmbedBuilder()
					{
						Title = "Closed tickets: ",
						Color = DiscordColor.Red,
						Description = message
					});
				}
			}
			else
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder()
				{
					Color = DiscordColor.Red,
					Description = "User does not have any closed tickets."
				});
			}
		}
	}
}
