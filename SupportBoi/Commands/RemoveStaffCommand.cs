using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace SupportBoi.Commands
{
	public class RemoveStaffCommand : BaseCommandModule
	{
		[Command("removestaff")]
		[Cooldown(1, 5, CooldownBucketType.User)]
		public async Task OnExecute(CommandContext command, [RemainingText] string commandArgs)
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
				command.Client.Logger.Log(LogLevel.Information, "User tried to use the removestaff command but did not have permission.");
				return;
			}

			ulong userID;
			string[] parsedMessage = Utilities.ParseIDs(command.RawArgumentString);

			if (!parsedMessage.Any())
			{
				userID = command.Member.Id;
			}
			else if (!ulong.TryParse(parsedMessage[0], out userID))
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
				deletion.ExecuteNonQuery();

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
