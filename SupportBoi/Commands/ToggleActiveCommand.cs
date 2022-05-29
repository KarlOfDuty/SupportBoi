using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace SupportBoi.Commands
{
	public class ToggleActiveCommand : BaseCommandModule
	{
		[Command("toggleactive")]
		[Aliases("ta")]
		public async Task OnExecute(CommandContext command, [RemainingText] string commandArgs)
		{
			if (!await Utilities.VerifyPermission(command, "toggleactive")) return;

			ulong staffID;
			string[] parsedMessage = Utilities.ParseIDs(command.RawArgumentString);

			if (!parsedMessage.Any())
			{
				staffID = command.Member.Id;
			}
			else if (!ulong.TryParse(parsedMessage[0], out staffID))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Invalid ID/Mention. (Could not convert to numerical)"
				};
				await command.RespondAsync(error);
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
				await command.RespondAsync(error);
				return;
			}

			if (Database.SetStaffActive(staffID, !staffMember.active))
			{
				DiscordEmbed message = new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Green,
                    Description = staffMember.active ? "Staff member is now set as inactive and will no longer be randomly assigned any support tickets." : "Staff member is now set as active and will be randomly assigned support tickets again."
                };
                await command.RespondAsync(message);
			}
			else
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error: Unable to update active status in database."
				};
				await command.RespondAsync(error);
			}
		}
	}
}
