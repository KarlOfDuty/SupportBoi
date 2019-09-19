using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MySql.Data.MySqlClient;

namespace SupportBoi.Commands
{
	public class SetTicketCommand
	{
		[Command("setticket")]
		[Description("Turns a channel into a ticket, warning: this will let anyone with write access delete the channel using the close command.")]
		public async Task OnExecute(CommandContext command)
		{
			using (MySqlConnection c = Database.GetConnection())
			{
				// Check if the user has permission to use this command.
				if (!Config.HasPermission(command.Member, "setticket"))
				{
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "You do not have permission to use this command."
					};
					await command.RespondAsync("", false, error);
					command.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi",
						"User tried to use command but did not have permission.", DateTime.UtcNow);
					return;
				}

				// Check if ticket exists in the database
				if (Database.IsTicket(command.Channel.Id))
				{
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "This channel is already a ticket."
					};
					await command.RespondAsync("", false, error);
					return;
				}

				c.Open();
				MySqlCommand cmd = new MySqlCommand(@"INSERT INTO tickets (created_time, creator_id, assigned_staff_id, summary, channel_id) VALUES (now(), @creator_id, @assigned_staff_id, @summary, @channel_id);", c);
				cmd.Parameters.AddWithValue("@creator_id", command.User.Id);
				cmd.Parameters.AddWithValue("@assigned_staff_id", 0);
				cmd.Parameters.AddWithValue("@summary", "");
				cmd.Parameters.AddWithValue("@channel_id", command.Channel.Id);
				cmd.ExecuteNonQuery();
				long id = cmd.LastInsertedId;
				string ticketID = id.ToString("00000");
				DiscordEmbed message = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = "Channel has been designated ticket " + ticketID + "."
				};
				await command.RespondAsync("", false, message);

				// Log it if the log channel exists
				DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
				if (logChannel != null)
				{
					DiscordEmbed logMessage = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Green,
						Description = command.Channel.Mention + " has been designated ticket " + ticketID + " by " + command.Member.Mention + "."
					};
					await logChannel.SendMessageAsync("", false, logMessage);
				}

				Sheets.AddTicketQueued(command.Member, command.Channel, id.ToString(), command.Member.Id.ToString(), command.Member.DisplayName);
			}
		}
	}
}
