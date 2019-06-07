using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MySql.Data.MySqlClient;
using MySql.Data.Types;

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
				MySqlCommand selection = new MySqlCommand(@"SELECT count(*) FROM tickets WHERE channel_id=@channel_id", c);
				selection.Parameters.AddWithValue("@channel_id", command.Channel.Id);
				selection.Prepare();
				long rows = (long)selection.ExecuteScalar();
				if (rows != 1)
				{
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "This channel is not a ticket."
					};
					await command.RespondAsync("", false, error);
					return;
				}

				string channelName = command.Channel.Name;
				DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);

				DiscordEmbed message = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = "Ticket closed by " + command.Member.Mention + ".\n",
					Footer = new DiscordEmbedBuilder.EmbedFooter{ Text = channelName }
				};

				await command.Channel.DeleteAsync("Ticket closed.");
				MySqlCommand deletion = new MySqlCommand(@"DELETE FROM tickets WHERE channel_id=@channel_id", c);
				deletion.Parameters.AddWithValue("@channel_id", command.Channel.Id);
				deletion.Prepare();
				deletion.ExecuteNonQuery();
				if (logChannel != null)
				{
					await logChannel.SendMessageAsync("", false, message);
				}
			}
		}
	}
}