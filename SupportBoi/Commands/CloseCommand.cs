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
			using (MySqlConnection c = Database.GetConnection())
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
					command.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi", "User tried to use command but did not have permission.", DateTime.Now);
					return;
				}

				ulong channelID = command.Channel.Id;
				string channelName = command.Channel.Name;
				c.Open();
				MySqlCommand selection = new MySqlCommand(@"SELECT * FROM tickets WHERE channel_id=@channel_id", c);
				selection.Parameters.AddWithValue("@channel_id", channelID);
				selection.Prepare();
				MySqlDataReader results = selection.ExecuteReader();

				// Check if ticket exists in the database
				if (!results.Read())
				{
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "This channel is not a ticket."
					};
					await command.RespondAsync("", false, error);
					return;
				}

				
				int id = results.GetInt32("id");
				string createdTime = results.GetString("created_time");
				ulong creatorID = results.GetUInt64("creator_id");
				ulong assignedStaffID = results.GetUInt64("assigned_staff_id");
				string summary = results.GetString("summary");

				// Build transcript
				string ticketNumber = id.ToString("00000");
				results.Close();
				string filePath = "";
				try
				{
					filePath = await Transcriber.ExecuteAsync(command.Channel.Id.ToString(), ticketNumber);
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

				// Log it if the log channel exists
				DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
				if (logChannel != null)
				{
					DiscordEmbed message = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Green,
						Description = "Ticket " + ticketNumber + " closed by " + command.Member.Mention + ".\n",
						Footer = new DiscordEmbedBuilder.EmbedFooter { Text = '#' + channelName }
					};
					await logChannel.SendFileAsync(filePath, "", false, message);
				}

				// Create an entry in the ticket history database
				MySqlCommand archiveTicket = new MySqlCommand(@"INSERT INTO ticket_history (id, created_time, closed_time, creator_id, assigned_staff_id, summary, channel_id) VALUES (@id, @created_time, now(), @creator_id, @assigned_staff_id, @summary, @channel_id);", c);
				archiveTicket.Parameters.AddWithValue("@id", id);
				archiveTicket.Parameters.AddWithValue("@created_time", createdTime);
				archiveTicket.Parameters.AddWithValue("@creator_id", creatorID);
				archiveTicket.Parameters.AddWithValue("@assigned_staff_id", assignedStaffID);
				archiveTicket.Parameters.AddWithValue("@summary", summary);
				archiveTicket.Parameters.AddWithValue("@channel_id", channelID);
				archiveTicket.ExecuteNonQuery();

				// Delete the channel and database entry
				await command.Channel.DeleteAsync("Ticket closed.");
				MySqlCommand deletion = new MySqlCommand(@"DELETE FROM tickets WHERE channel_id=@channel_id", c);
				deletion.Parameters.AddWithValue("@channel_id", channelID);
				deletion.Prepare();
				deletion.ExecuteNonQuery();
			}
		}
	}
}
