using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MySql.Data.MySqlClient;

namespace SupportBoi.Commands
{
	public class RemoveStaffCommand
	{
		[Command("removestaff")]
		[Cooldown(1, 5, CooldownBucketType.User)]
		public async Task OnExecute(CommandContext command)
		{
			// Check if the user has permission to use this command.
			if (!Config.HasPermission(command.Member, "removestaff"))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "You do not have permission to use this command."
				};
				await command.RespondAsync("", false, error);
				command.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi",
					"User tried to use the removestaff command but did not have permission.", DateTime.UtcNow);
				return;
			}

			string strippedMessage = command.Message.Content.Replace(Config.prefix, "");
			string[] parsedMessage = strippedMessage.Replace("<@", "").Replace(">", "").Split();

			if (parsedMessage.Length < 2)
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "You need to provide an ID/Mention."
				};
				await command.RespondAsync("", false, error);
				return;
			}

			if (!ulong.TryParse(parsedMessage[1], out ulong userID))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Invalid ID/Mention. (Could not convert to numerical)"
				};
				await command.RespondAsync("", false, error);
				return;
			}

			try
			{
				await command.Client.GetUserAsync(userID);
			}
			catch (Exception)
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Invalid ID/Mention. (Could not find user on Discord)"
				};
				await command.RespondAsync("", false, error);
				return;
			}

			if (!Database.IsStaff(userID))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "User is already not registered as staff."
				};
				await command.RespondAsync("", false, error);
				return;
			}

			using (MySqlConnection c = Database.GetConnection())
			{
				c.Open();
				MySqlCommand deletion = new MySqlCommand(@"DELETE FROM staff WHERE user_id=@user_id", c);
				deletion.Parameters.AddWithValue("@user_id", userID);
				deletion.Prepare();

				DiscordEmbed message = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = "User was removed from staff."
				};
				await command.RespondAsync("", false, message);

				// Log it if the log channel exists
				DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
				if (logChannel != null)
				{
					DiscordEmbed logMessage = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Green,
						Description = "User was removed from staff.\n",
					};
					await logChannel.SendMessageAsync("", false, logMessage);
				}
			}
		}
	}
}
