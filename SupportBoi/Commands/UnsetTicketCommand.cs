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
		[Description(
			"Deletes a channel from the ticket system without deleting the channel.")]
		public async Task OnExecute(CommandContext command, [RemainingText] string commandArgs)
		{
			using (MySqlConnection c = Database.GetConnection())
			{
				// Check if the user has permission to use this command.
				if (!Config.HasPermission(command.Member, "unsetticket"))
				{
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "You do not have permission to use this command."
					};
					await command.RespondAsync(error);
					command.Client.Logger.Log(LogLevel.Information, "User tried to use the unsetticket command but did not have permission.");
					return;
				}

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

				c.Open();
				MySqlCommand deletion = new MySqlCommand(@"DELETE FROM tickets WHERE channel_id=@channel_id", c);
				deletion.Parameters.AddWithValue("@channel_id", command.Channel.Id);
				deletion.Prepare();
				deletion.ExecuteNonQuery();
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
		}
	}
}
