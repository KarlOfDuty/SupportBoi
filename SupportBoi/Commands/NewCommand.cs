using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MySql.Data.MySqlClient;

namespace SupportBoi.Commands
{
	public class NewCommand
	{
		[Command("new")]
		[Cooldown(1, 5, CooldownBucketType.User)]
		public async Task OnExecute(CommandContext command)
		{
			using (MySqlConnection c = Database.GetConnection())
			{
				// Check if the user has permission to use this command.
				if (!Config.HasPermission(command.Member, "new"))
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

				// Check if user is blacklisted
				if (Database.IsBlacklisted(command.User.Id))
				{
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "You are banned from opening tickets."
					};
					await command.RespondAsync("", false, error);
					return;
				}

				DiscordChannel category = command.Guild.GetChannel(Config.ticketCategory);
				DiscordChannel ticketChannel;

				try
				{
					ticketChannel = await command.Guild.CreateChannelAsync("ticket", ChannelType.Text, category);

				}
				catch (Exception)
				{
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "Error occured while creating ticket, " + command.Member.Mention + "!\nIs the channel limit reached in the server or ticket category?"
					};
					await command.RespondAsync("", false, error);
					return;
				}


				if (ticketChannel == null)
				{
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "Error occured while creating ticket, " + command.Member.Mention + "!\nIs the channel limit reached in the server or ticket category?"
					};
					await command.RespondAsync("", false, error);
					return;
				}

				c.Open();
				MySqlCommand cmd = new MySqlCommand(@"INSERT INTO tickets (created_time, creator_id, assigned_staff_id, summary, channel_id) VALUES (now(), @creator_id, @assigned_staff_id, @summary, @channel_id);", c);
				cmd.Parameters.AddWithValue("@creator_id", command.User.Id);
				cmd.Parameters.AddWithValue("@assigned_staff_id", 0);
				cmd.Parameters.AddWithValue("@summary", "");
				cmd.Parameters.AddWithValue("@channel_id", ticketChannel.Id);
				cmd.ExecuteNonQuery();
				long id = cmd.LastInsertedId;
				string ticketID = id.ToString("00000");
				await ticketChannel.ModifyAsync("ticket-" + ticketID);
				await ticketChannel.AddOverwriteAsync(command.Member, Permissions.AccessChannels, Permissions.None);

				await ticketChannel.SendMessageAsync("Hello, " + command.Member.Mention + "!\n" + Config.welcomeMessage);

				// Adds the ticket to the google sheets document if enabled
				if (Config.sheetsEnabled)
				{
					// Refreshes the channel as changes were made to it above
					ticketChannel = command.Guild.GetChannel(ticketChannel.Id);
					Sheets.AddTicket(command.Member, ticketChannel, ticketID);
				}
				
				DiscordEmbed message = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = "Ticket opened, " + command.Member.Mention + "!\n" + ticketChannel.Mention
				};
				await command.RespondAsync("", false, message);

				// Log it if the log channel exists
				DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
				if (logChannel != null)
				{
					DiscordEmbed logMessage = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Green,
						Description = "Ticket " + ticketChannel.Mention + " opened by " + command.Member.Mention + ".\n",
						Footer = new DiscordEmbedBuilder.EmbedFooter { Text = "Ticket " + ticketID }
					};
					await logChannel.SendMessageAsync("", false, logMessage);
				}
			}
		}
	}
}
