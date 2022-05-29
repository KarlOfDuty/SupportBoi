using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace SupportBoi.Commands
{
	public class ReloadCommand : BaseCommandModule
	{
		[Command("reload")]
		public async Task OnExecute(CommandContext command, [RemainingText] string commandArgs)
		{
			if (!await Utilities.VerifyPermission(command, "reload")) return;

			DiscordEmbed message = new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Reloading bot application..."
			};
			await command.RespondAsync(message);
			Logger.Log(LogID.DISCORD, "Reloading bot...");
			SupportBoi.Reload();
		}
	}
}
