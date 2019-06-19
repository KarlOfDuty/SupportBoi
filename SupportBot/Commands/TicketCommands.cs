using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace SupportBot.Commands
{
	public class TicketCommands
	{
		[Command("new")]
		public async Task New(CommandContext command)
		{
			using (MySqlConnection c = Database.GetConnection())
			{
				DiscordChannel category = command.Guild.GetChannel(Config.ticketCategory);
				DiscordChannel ticketChannel = await command.Guild.CreateChannelAsync("ticket", ChannelType.Text, category);

				if (ticketChannel == null)
				{
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "Could not open ticket, " + command.Member.Mention + "!"
					};
					await command.RespondAsync("", false, error);
					return;
				}

				c.Open();
				MySqlCommand cmd = new MySqlCommand(@"INSERT INTO tickets (created_time,creator_id,channel_id) VALUES (now(), @creator_id, @channel_id);", c);
				cmd.Parameters.AddWithValue("@creator_id", command.User.Id);
				cmd.Parameters.AddWithValue("@channel_id", ticketChannel.Id);
				cmd.ExecuteNonQuery();
				long id = cmd.LastInsertedId;
				string ticketID = id.ToString("00000");
				await ticketChannel.ModifyAsync("ticket-" + ticketID);
				await ticketChannel.AddOverwriteAsync(command.Member,Permissions.AccessChannels, Permissions.None);
				DiscordEmbed message = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = "Ticket opened, " + command.Member.Mention + "!\n" + ticketChannel.Mention
				};//.AddField(":point_down:", ticketChannel.Mention);
				await command.RespondAsync("", false, message);
			}
		}
		[Command("close")]
		public async Task Close(CommandContext command)
		{
			using (MySqlConnection c = Database.GetConnection())
			{
				c.Open();
				MySqlCommand selection = new MySqlCommand(@"SELECT * FROM tickets WHERE channel_id=@channel_id", c);
				selection.Parameters.AddWithValue("@channel_id", command.Channel.Id);
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

				// Build transcript
				string ticketNumber = results.GetInt32("id").ToString("00000");

				// As Discord only allows reading of 100 messages at a time they have to be read in this slightly clunky way
				LinkedList<DiscordMessage> allMessages = new LinkedList<DiscordMessage>();
				IReadOnlyList<DiscordMessage> messages = null;
				do
				{
					messages = await command.Channel.GetMessagesAsync(100, null, messages?.Last()?.ChannelId);
					foreach (DiscordMessage message in messages)
					{
						allMessages.AddFirst(message);
					}
				}
				while (messages?.Count == 100);

				
				// Automatically serializes the list, might not be the best way to do things as all message info is serialized and everything is not needed, although it makes tickets way more future-proof
				string jsonString = JsonConvert.SerializeObject(allMessages);
				
				if (!Directory.Exists("./transcripts"))
				{
					Directory.CreateDirectory("./transcripts");
				}

				try
				{
					// The transcripts are saved as pure json data instead of html so the transcript design can change in the future
					File.WriteAllText("./transcripts/" + ticketNumber + ".json", jsonString);
				}
				catch (Exception e)
				{
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "ERROR: Could not save transcript file. Aborting..."
					};
					await command.RespondAsync("", false, error);
					command.Client.DebugLogger.LogMessage(LogLevel.Error, "SupportBot", "Error occured while creating transcript file:\n" + e, DateTime.Now);
					return;
				}

				// Delete the channel and database entry
				await command.Channel.DeleteAsync("Ticket closed.");
				MySqlCommand deletion = new MySqlCommand(@"DELETE FROM tickets WHERE channel_id=@channel_id", c);
				deletion.Parameters.AddWithValue("@channel_id", command.Channel.Id);
				deletion.Prepare();
				deletion.ExecuteNonQuery();

				// Log it if the log channel exists
				string channelName = command.Channel.Name;
				DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
				if (logChannel != null)
				{
					DiscordEmbed message = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Green,
						Description = "Ticket " + ticketNumber + " closed by " + command.Member.Mention + ".\n",
						Footer = new DiscordEmbedBuilder.EmbedFooter{ Text = '#' + channelName }
					};
					await logChannel.SendMessageAsync("", false, message);
				}
			}
		}
	}
}