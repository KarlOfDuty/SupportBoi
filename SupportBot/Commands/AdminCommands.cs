using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using MySql.Data.MySqlClient;

namespace SupportBot.Commands
{
	[Description("Admin commands.")]
	[Hidden]
	[Cooldown(1, 10, CooldownBucketType.User)]
	public class AdminCommands
	{
		[Command("reload")]
		public async Task Reload(CommandContext command)
		{
			// Check if the user has permission to use this command.
			if (!Config.IsAdmin(command.Member.Roles))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "You do not have permission to use this command."
				};
				await command.RespondAsync("", false, error);
				command.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBot", "User tried to use command but did not have permission.", DateTime.Now);
				return;
			}

			DiscordEmbed message = new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Reloading bot application..."
			};
			await command.RespondAsync("", false, message);
			Console.WriteLine("Reloading bot...");
			SupportBot.instance.Reload();
		}

		[Command("setticket")]
		[Description(
			"Turns a channel into a ticket, warning: this will let anyone with write access delete the channel using the close command.")]
		public async Task SetTicket(CommandContext command)
		{
			using (MySqlConnection c = Database.GetConnection())
			{
				// Check if the user has permission to use this command.
				if (!Config.IsAdmin(command.Member.Roles))
				{
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "You do not have permission to use this command."
					};
					await command.RespondAsync("", false, error);
					command.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBot",
						"User tried to use command but did not have permission.", DateTime.Now);
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
				MySqlCommand cmd = new MySqlCommand(@"INSERT INTO tickets (created_time,creator_id,channel_id) VALUES (now(), @creator_id, @channel_id);", c);
				cmd.Parameters.AddWithValue("@creator_id", command.User.Id);
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
			}
		}

		[Command("unsetticket")]
		[Description(
			"Deletes a channel from the ticket system without deleting the channel.")]
		public async Task UnSetTicket(CommandContext command)
		{
			using (MySqlConnection c = Database.GetConnection())
			{
				// Check if the user has permission to use this command.
				if (!Config.IsAdmin(command.Member.Roles))
				{
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "You do not have permission to use this command."
					};
					await command.RespondAsync("", false, error);
					command.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBot",
						"User tried to use command but did not have permission.", DateTime.Now);
					return;
				}

				// Check if ticket exists in the database
				if (!Database.IsTicket(command.Channel.Id))
				{
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "This channel is not a ticket."
					};
					await command.RespondAsync("", false, error);
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
				await command.RespondAsync("", false, message);

				// Log it if the log channel exists
				DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
				if (logChannel != null)
				{
					DiscordEmbed logMessage = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Green,
						Description = command.Channel.Mention + " has been undesignated as a ticket by " + command.Member.Mention + "."
					};
					await logChannel.SendMessageAsync("", false, logMessage);
				}
			}
		}
	}
}
