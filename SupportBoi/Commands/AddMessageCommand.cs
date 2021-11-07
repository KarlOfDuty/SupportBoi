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
	public class AddMessageCommand : BaseCommandModule
	{
		[Command("addmessage")]
		[Description("Adds a new message for the 'say' command.")]
		public async Task OnExecute(CommandContext command, string identifier, [RemainingText] string message)
		{
			// Check if the user has permission to use this command.
			if (!Config.HasPermission(command.Member, "addmessage"))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "You do not have permission to use this command."
				};
				await command.RespondAsync(error);
				command.Client.Logger.Log(LogLevel.Information, "User tried to use the addmessage command but did not have permission.");
				return;
			}

			if (string.IsNullOrEmpty(message))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "No message specified."
				};
				await command.RespondAsync(error);
				return;
			}

			if (Database.TryGetMessage(identifier.ToLower(), out Database.Message _))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "There is already a message with that identifier."
				};
				await command.RespondAsync(error);
				return;
			}

			if(Database.AddMessage(identifier, command.Member.Id, message))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = "Message added."
				};
				await command.RespondAsync(error);
				return;
			}
			else
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error: Failed adding the message to the database."
				};
				await command.RespondAsync(error);
				return;
			}

		}
	}
}
