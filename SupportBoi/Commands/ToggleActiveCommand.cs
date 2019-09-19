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
		[Aliases("ta")]
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
					command.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi", "User tried to use the toggleactive command but did not have permission.", DateTime.UtcNow);
					return;
				}

				ulong staffID;
				string strippedMessage = command.Message.Content.Replace(Config.prefix, "");
				string[] parsedMessage = strippedMessage.Replace("<@", "").Replace(">", "").Split();
				
				if (parsedMessage.Length < 2)
				{
					staffID = command.Member.Id;
				}
				else if (!ulong.TryParse(parsedMessage[1], out staffID))
				{
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "Invalid ID/Mention. (Could not convert to numerical)"
					};
					await command.RespondAsync("", false, error);
					return;
				}

				// Check if ticket exists in the database
				if (!Database.TryGetStaff(staffID, out Database.StaffMember staffMember))
				{
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "You have not been registered as staff."
					};
					await command.RespondAsync("", false, error);
					return;
				}

				c.Open();
				MySqlCommand update = new MySqlCommand(@"UPDATE staff SET active = @active WHERE user_id = @user_id", c);
				update.Parameters.AddWithValue("@user_id", staffID);
				update.Parameters.AddWithValue("@active", !staffMember.active);
				update.Prepare();
				update.ExecuteNonQuery();

				DiscordEmbed message = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = staffMember.active ? "Staff member is now set as inactive and will no longer be randomly assigned any support tickets." : "Staff member is now set as active and will be randomly assigned support tickets again."
				};
				await command.RespondAsync("", false, message);
			}
		}
	}
}
