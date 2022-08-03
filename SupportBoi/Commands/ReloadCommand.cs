using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace SupportBoi.Commands
{
	public class ReloadCommand : ApplicationCommandModule
	{
		[Config.ConfigPermissionCheckAttribute("reload")]
		[SlashCommand("reload", "Reloads the bot config.")]
		public async Task OnExecute(InteractionContext command)
		{
			await command.CreateResponseAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Reloading bot application..."
			});
			Logger.Log(LogID.DISCORD, "Reloading bot...");
			SupportBoi.Reload();
		}
	}
}
