using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MySql.Data.MySqlClient;

namespace SupportBoi.Commands
{
	public class ToggleActiveCommand
	{
		[Command("toggleactive")]
		[Cooldown(1, 5, CooldownBucketType.User)]
		public async Task OnExecute(CommandContext command)
		{
			using (MySqlConnection c = Database.GetConnection())
			{
				// Check if the user has permission to use this command.
				if (!Config.HasPermission(command.Member, "toggleactive"))
				{
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "You do not have permission to use this command."
					};
					await command.RespondAsync("", false, error);
					command.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi", "User tried to use the toggleactive command but did not have permission.", DateTime.Now);
					return;
				}

				c.Open();
				MySqlCommand selection = new MySqlCommand(@"SELECT * FROM staff_list WHERE user_id=@user_id", c);
				selection.Parameters.AddWithValue("@user_id", command.Member.Id);
				selection.Prepare();
				MySqlDataReader results = selection.ExecuteReader();

				// Check if ticket exists in the database
				if (!results.Read())
				{
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "You have not been registered as staff."
					};
					await command.RespondAsync("", false, error);
					return;
				}

				bool isActive = results.GetBoolean("active");
				results.Close();

				MySqlCommand update = new MySqlCommand(@"UPDATE staff_list SET active = @active WHERE user_id = @user_id", c);
				update.Parameters.AddWithValue("@user_id", command.Member.Id);
				update.Parameters.AddWithValue("@active", !isActive);
				update.Prepare();
				update.ExecuteNonQuery();

				DiscordEmbed message = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = isActive ? "You are now set as inactive and will no longer be randomly assigned any support tickets." : "You are now set as active and will be randomly assigned support tickets again."
				};
				await command.RespondAsync("", false, message);
			}
		}
	}
}
