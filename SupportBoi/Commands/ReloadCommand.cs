using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace SupportBoi.Commands
{
	public class ReloadCommand
	{
		[Command("reload")]
		public async Task OnExecute(CommandContext command)
		{
			// Check if the user has permission to use this command.
			if (!Config.HasPermission(command.Member, "reload"))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "You do not have permission to use this command."
				};
				await command.RespondAsync("", false, error);
				command.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi", "User tried to use the reload command but did not have permission.", DateTime.Now);
				return;
			}

			DiscordEmbed message = new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Reloading bot application..."
			};
			await command.RespondAsync("", false, message);
			Console.WriteLine("Reloading bot...");
			SupportBoi.instance.Reload();
		}
	}
}
