using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using MySql.Data.MySqlClient;

namespace SupportBoi.Commands
{
	public class AddStaffCommand : ApplicationCommandModule
	{
		[SlashRequireGuild]
		[Config.ConfigPermissionCheckAttribute("addstaff")]
		[SlashCommand("addstaff", "Adds a new staff member.")]
		public async Task OnExecute(InteractionContext command, [Option("User", "User to add to staff.")] DiscordUser user)
		{
			DiscordMember staffMember = null;
			try
			{
				staffMember = user == null ? command.Member : await command.Guild.GetMemberAsync(user.Id);

				if (staffMember == null)
				{
					await command.CreateResponseAsync(new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "Could not find that user in this server."
					}, true);
					return;
				}
			}
			catch (Exception)
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Could not find that user in this server."
				}, true);
				return;
			}

			await using MySqlConnection c = Database.GetConnection();
			MySqlCommand cmd = Database.IsStaff(staffMember.Id) ? new MySqlCommand(@"UPDATE staff SET name = @name WHERE user_id = @user_id", c) : new MySqlCommand(@"INSERT INTO staff (user_id, name) VALUES (@user_id, @name);", c);

			c.Open();
			cmd.Parameters.AddWithValue("@user_id", staffMember.Id);
			cmd.Parameters.AddWithValue("@name", staffMember.DisplayName);
			cmd.ExecuteNonQuery();
			cmd.Dispose();

			await command.CreateResponseAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = staffMember.Mention + " was added to staff."
			});

			// Log it if the log channel exists
			DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
			if (logChannel != null)
			{
				await logChannel.SendMessageAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = staffMember.Mention + " was added to staff.\n",
				});
			}
		}
	}
}
