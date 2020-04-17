using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SupportBoi.Commands
{
	public class ToggleActiveCommand
	{
		[Command("toggleactive")]
		[Aliases("ta")]
		public async Task OnExecute(CommandContext command)
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

			Database.UpdateStaffActive(staffID, !staffMember.active);

			DiscordEmbed message = new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = staffMember.active ? "Staff member is now set as inactive and will no longer be randomly assigned any support tickets." : "Staff member is now set as active and will be randomly assigned support tickets again."
			};
			await command.RespondAsync("", false, message);
		}
	}
}
