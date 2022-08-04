using DSharpPlus.Entities;
using System.Threading.Tasks;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SupportBoi.Commands
{
	public class RemoveMessageCommand : ApplicationCommandModule
	{
		[SlashRequireGuild]
		[Config.ConfigPermissionCheckAttribute("removemessage")]
		[SlashCommand("removemessage", "Removes a message from the 'say' command.")]
		public async Task OnExecute(InteractionContext command, [Option("Identifier", "The identifier word used in the /say command.")] string identifier)
		{
			if (!Database.TryGetMessage(identifier.ToLower(), out Database.Message _))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "There is no message with that identifier."
				}, true);
				return;
			}

			if (Database.RemoveMessage(identifier))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = "Message removed."
				}, true);
			}
			else
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error: Failed removing the message from the database."
				}, true);
			}
		}
	}
}
