using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SupportBoi.Commands
{
	public class UnsetTicketCommand : ApplicationCommandModule
	{
		[SlashRequireGuild]
		[Config.ConfigPermissionCheckAttribute("unsetticket")]
		[SlashCommand("unsetticket", "Deletes a channel from the ticket system without deleting the channel.")]
		public async Task OnExecute(InteractionContext command)
		{
			// Check if ticket exists in the database
			if (!Database.TryGetOpenTicket(command.Channel.Id, out Database.Ticket ticket))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "This channel is not a ticket."
				}, true);
				return;
			}

			if (Database.DeleteOpenTicket(ticket.id))
			{
                await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = "Channel has been undesignated as a ticket."
				});
    
                // Log it if the log channel exists
                DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
                if (logChannel != null)
                {
                	await logChannel.SendMessageAsync(new DiscordEmbedBuilder
					{
						Color = DiscordColor.Green,
						Description = command.Channel.Mention + " has been undesignated as a ticket by " + command.Member.Mention + "."
					});
                }
			}
			else
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error: Failed removing ticket from database."
				}, true);
			}
		}
	}
}
