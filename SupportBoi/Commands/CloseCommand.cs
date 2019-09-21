using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MySql.Data.MySqlClient;

namespace SupportBoi.Commands
{
	public class CloseCommand
	{
		[Command("close")]
		[Cooldown(1, 5, CooldownBucketType.User)]
		public async Task OnExecute(CommandContext command)
		{
			// Check if the user has permission to use this command.
			if (!Config.HasPermission(command.Member, "close"))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "You do not have permission to use this command."
				};
				await command.RespondAsync("", false, error);
				command.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi", "User tried to use the close command but did not have permission.", DateTime.UtcNow);
				return;
			}

			ulong channelID = command.Channel.Id;
			string channelName = command.Channel.Name;

			// Check if ticket exists in the database
			if (!Database.TryGetOpenTicket(channelID, out Database.Ticket ticket))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "This channel is not a ticket."
				};
				await command.RespondAsync("", false, error);
				return;
			}

			// Build transcript
			try
			{
				Transcriber.ExecuteAsync(command.Channel.Id.ToString(), ticket.id);
			}
			catch (Exception)
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "ERROR: Could not save transcript file. Aborting..."
				};
				await command.RespondAsync("", false, error);
				throw;
			}
			string filePath = Transcriber.GetPath(ticket.id);

			// Log it if the log channel exists
			DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
			if (logChannel != null)
			{
				DiscordEmbed message = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = "Ticket " + ticket.id.ToString("00000") + " closed by " + command.Member.Mention + ".\n",
					Footer = new DiscordEmbedBuilder.EmbedFooter { Text = '#' + channelName }
				};
				await logChannel.SendFileAsync(filePath, "", false, message);
			}

			using (MySqlConnection c = Database.GetConnection())
			{
				// Create an entry in the ticket history database
				MySqlCommand archiveTicket = new MySqlCommand(@"INSERT INTO ticket_history (id, created_time, closed_time, creator_id, assigned_staff_id, summary, channel_id) VALUES (@id, @created_time, now(), @creator_id, @assigned_staff_id, @summary, @channel_id);", c);
				archiveTicket.Parameters.AddWithValue("@id", ticket.id);
				archiveTicket.Parameters.AddWithValue("@created_time", ticket.createdTime);
				archiveTicket.Parameters.AddWithValue("@creator_id", ticket.creatorID);
				archiveTicket.Parameters.AddWithValue("@assigned_staff_id", ticket.assignedStaffID);
				archiveTicket.Parameters.AddWithValue("@summary", ticket.summary);
				archiveTicket.Parameters.AddWithValue("@channel_id", channelID);

				c.Open();
				archiveTicket.ExecuteNonQuery();

				// Delete the channel and database entry
				await command.Channel.DeleteAsync("Ticket closed.");
				MySqlCommand deletion = new MySqlCommand(@"DELETE FROM tickets WHERE channel_id=@channel_id", c);
				deletion.Parameters.AddWithValue("@channel_id", channelID);
				deletion.Prepare();
				deletion.ExecuteNonQuery();

				Sheets.DeleteTicketQueued(ticket.id);
			}
		}
	}
}
