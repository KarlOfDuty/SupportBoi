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

			ulong userID;
			string[] parsedArgs = Utilities.ParseIDs(command.RawArgumentString);

			if (!parsedArgs.Any())
			{
				userID = command.Member.Id;
			}
			else if (!ulong.TryParse(parsedArgs[0], out userID))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Invalid ID/Mention. (Could not convert to numerical)"
				};
				await command.RespondAsync("", false, error);
				return;
			}

			DiscordMember member;
			try
			{
				member = await command.Guild.GetMemberAsync(userID);
			}
			catch (Exception)
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Invalid ID/Mention. (Could not find user on this server)"
				};
				await command.RespondAsync("", false, error);
				return;
			}

			using (MySqlConnection c = Database.GetConnection())
			{
				MySqlCommand cmd = Database.IsStaff(userID) ? new MySqlCommand(@"UPDATE staff SET name = @name WHERE user_id = @user_id", c) : new MySqlCommand(@"INSERT INTO staff (user_id, name) VALUES (@user_id, @name);", c);

				c.Open();
				cmd.Parameters.AddWithValue("@user_id", userID);
				cmd.Parameters.AddWithValue("@name", member.DisplayName);
				cmd.ExecuteNonQuery();
				cmd.Dispose();

				DiscordEmbed message = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = member.Mention + " was added to staff."
				};
				await command.RespondAsync("", false, message);

				// Log it if the log channel exists
				DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
				if (logChannel != null)
				{
					DiscordEmbed logMessage = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Green,
						Description = member.Mention + " was added to staff.\n",
					};
					await logChannel.SendMessageAsync("", false, logMessage);
				}
			}
		}
	}
}
