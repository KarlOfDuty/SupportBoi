using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace SupportBot.Commands
{
	[Description("Admin commands.")]
	[Hidden]
	[RequirePermissions(Permissions.ManageGuild)]
	public class AdminCommands
	{
		[Command("setlogchannel")]
		[RequirePermissions(Permissions.Administrator)]
		public async Task SetLogChannel(CommandContext command, string id)
		{
			await command.RespondAsync("Received setlogchannel command");
		}

		[Command("addrole")]
		[RequirePermissions(Permissions.Administrator)]
		public async Task AddRole(CommandContext command)
		{
			await command.RespondAsync("Received addrole command");
		}

		[Command("removerole")]
		[RequirePermissions(Permissions.Administrator)]
		public async Task RemoveRole(CommandContext command)
		{
			await command.RespondAsync("Received removerole command");
		}

		[Command("addcategory")]
		public async Task AddCategory(CommandContext command, string id, string name)
		{
			await command.RespondAsync("Received addcategory command");
		}
		[Command("removecategory")]
		public async Task RemoveCategory(CommandContext command, string id)
		{
			await command.RespondAsync("Received removecategory command");
		}

		[Command("reload")]
		public async Task Reload(CommandContext command)
		{
			Config.LoadConfig();
			DiscordEmbed message = new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Config reloaded."
			};
			await command.RespondAsync("", false, message);
		}
	}
}
