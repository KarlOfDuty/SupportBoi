using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MySql.Data.MySqlClient;

namespace SupportBoi.Commands
{
	public class AddStaffCommand
	{
		[Command("addstaff")]
		[Cooldown(1, 5, CooldownBucketType.User)]
		public async Task OnExecute(CommandContext command)
		{
			using (MySqlConnection c = Database.GetConnection())
			{
				// Check if the user has permission to use this command.
				if (!Config.HasPermission(command.Member, "addstaff"))
				{
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "You do not have permission to use this command."
					};
					await command.RespondAsync("", false, error);
					command.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi", "User tried to use the addstaff command but did not have permission.", DateTime.Now);
					return;
				}

				string[] parsedMessage = command.Message.Content.Remove(0, (Config.prefix + "addstaff ").Length).Replace("<@", "").Replace(">", "").Split();

				if (parsedMessage.Length < 2)
				{
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "You need to provide an ID/Mention followed by a nickname."
					};
					await command.RespondAsync("", false, error);
					return;
				}

				if (!ulong.TryParse(parsedMessage[0], out ulong userID))
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

				string username = string.Join('_', parsedMessage.Skip(1));

				c.Open();
				MySqlCommand cmd = new MySqlCommand(@"INSERT INTO staff_list (user_id, username) VALUES (@user_id, @username);", c);
				cmd.Parameters.AddWithValue("@user_id", userID);
				cmd.Parameters.AddWithValue("@username", username);
				cmd.ExecuteNonQuery();

				DiscordEmbed message = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = "Staff member added."
				};
				await command.RespondAsync("", false, message);

				// Log it if the log channel exists
				DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
				if (logChannel != null)
				{
					DiscordEmbed logMessage = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Green,
						Description = command.Member.Mention + " was added to the staff list.\n",
					};
				}
			}
		}
	}
}
