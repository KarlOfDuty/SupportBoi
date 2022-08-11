using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using MySql.Data.MySqlClient;

namespace SupportBoi.Commands
{
	public class RemoveStaffCommand : ApplicationCommandModule
	{
		[SlashRequireGuild]
		[SlashCommand("removestaff", "Removes a staff member.")]
		public async Task OnExecute(InteractionContext command, [Option("User", "User to remove from staff.")] DiscordUser user)
		{
			if (!Database.IsStaff(user.Id))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "User is already not registered as staff."
				}, true);
				return;
			}

			using (MySqlConnection c = Database.GetConnection())
			{
				c.Open();
				MySqlCommand deletion = new MySqlCommand(@"DELETE FROM staff WHERE user_id=@user_id", c);
				deletion.Parameters.AddWithValue("@user_id", user.Id);
				deletion.Prepare();
				deletion.ExecuteNonQuery();

				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = "User was removed from staff."
				}, true);

				// Log it if the log channel exists
				DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
				if (logChannel != null)
				{
					await logChannel.SendMessageAsync(new DiscordEmbedBuilder
					{
						Color = DiscordColor.Green,
						Description = "User was removed from staff.\n",
					});
				}
			}
		}
	}
}
