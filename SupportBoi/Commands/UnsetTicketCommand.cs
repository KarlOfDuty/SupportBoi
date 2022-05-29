using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace SupportBoi.Commands
{
	public class UnsetTicketCommand : BaseCommandModule
	{
		[Command("unsetticket")]
		[Description("Deletes a channel from the ticket system without deleting the channel.")]
		public async Task OnExecute(CommandContext command, [RemainingText] string commandArgs)
		{
			if (!await Utilities.VerifyPermission(command, "unsetticket")) return;

			// Check if ticket exists in the database
			if (!Database.TryGetOpenTicket(command.Channel.Id, out Database.Ticket ticket))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "This channel is not a ticket."
				};
				await command.RespondAsync(error);
				return;
			}

			if (Database.DeleteOpenTicket(ticket.id))
			{
				DiscordEmbed message = new DiscordEmbedBuilder
                {
                	Color = DiscordColor.Green,
                	Description = "Channel has been undesignated as a ticket."
                };
                await command.RespondAsync(message);
    
                // Log it if the log channel exists
                DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
                if (logChannel != null)
                {
                	DiscordEmbed logMessage = new DiscordEmbedBuilder
                	{
                		Color = DiscordColor.Green,
                		Description = command.Channel.Mention + " has been undesignated as a ticket by " + command.Member.Mention + "."
                	};
                	await logChannel.SendMessageAsync(logMessage);
                }
			}
			else
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error: Failed removing ticket from database."
				};
				await command.RespondAsync(error);
			}
		}
	}
}
