using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SupportBoi.Commands
{
	public class SetTicketCommand : ApplicationCommandModule
	{
		[SlashRequireGuild]
		[Config.ConfigPermissionCheckAttribute("setticket")]
		[SlashCommand("setticket", "Turns a channel into a ticket, warning: this will let anyone with write access delete the channel using the close command.")]
		public async Task OnExecute(InteractionContext command, DiscordUser user = null)
		{
			// Check if ticket exists in the database
			if (Database.IsOpenTicket(command.Channel.Id))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "This channel is already a ticket."
				}, true);
				return;
			}

			DiscordUser ticketUser = (user == null ? command.User : user);

			long id = Database.NewTicket(ticketUser.Id, 0, command.Channel.Id);
			string ticketID = id.ToString("00000");
			await command.CreateResponseAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Channel has been designated ticket " + ticketID + "."
			});

			// Log it if the log channel exists
			DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
			if (logChannel != null)
			{
				await logChannel.SendMessageAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = command.Channel.Mention + " has been designated ticket " + ticketID + " by " + command.Member.Mention + "."
				});
			}
		}
	}
}
