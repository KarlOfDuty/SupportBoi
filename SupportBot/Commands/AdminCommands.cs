using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;

namespace SupportBot.Commands
{
	[Description("Admin commands.")]
	[Hidden]
	[Cooldown(1, 10, CooldownBucketType.User)]
	public class AdminCommands
	{
		[Command("reload")]
		public async Task Reload(CommandContext command)
		{
			IEnumerable<DiscordRole> roles = command.Member.Roles;

			// Check if the user has permission to use this command.
			if (roles.All(x => x.Id != Config.adminRole))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "You do not have permission to use this command."
				};
				await command.RespondAsync("", false, error);
				command.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBot", "User tried to use command but did not have permission.", DateTime.Now);
				return;
			}

			DiscordEmbed message = new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Reloading bot application..."
			};
			await command.RespondAsync("", false, message);
			Console.WriteLine("Reloading bot...");
			SupportBot.instance.Reload();
		}
	}
}
