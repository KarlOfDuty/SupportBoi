using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportBoi.Commands
{
	public class RemoveMessageCommand : BaseCommandModule
	{
		[Command("removemessage")]
		[Description("Removes a message from the 'say' command.")]
		public async Task OnExecute(CommandContext command, string identifier)
		{
			// Check if the user has permission to use this command.
			if (!Config.HasPermission(command.Member, "removemessage"))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "You do not have permission to use this command."
				};
				await command.RespondAsync(error);
				command.Client.Logger.Log(LogLevel.Information, "User tried to use the removemessage command but did not have permission.");
				return;
			}

			if (!Database.TryGetMessage(identifier.ToLower(), out Database.Message _))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "There is no message with that identifier."
				};
				await command.RespondAsync(error);
				return;
			}

			if(Database.RemoveMessage(identifier))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = "Message removed."
				};
				await command.RespondAsync(error);
				return;
			}
			else
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error: Failed removing the message from the database."
				};
				await command.RespondAsync(error);
				return;
			}

		}
	}
}
