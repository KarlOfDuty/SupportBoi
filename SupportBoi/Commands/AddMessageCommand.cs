using DSharpPlus.Entities;
using System.Threading.Tasks;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SupportBoi.Commands
{
	public class AddMessageCommand : ApplicationCommandModule
	{
		[SlashRequireGuild]
		[Config.ConfigPermissionCheckAttribute("addmessage")]
		[SlashCommand("addmessage", "Adds a new message for the 'say' command.")]
		public async Task OnExecute(InteractionContext command, 
			[Option("Identifier", "The identifier word used in the /say command.")] string identifier, 
			[Option("Message", "The message the /say command will return.")] string message)
		{
			if (string.IsNullOrEmpty(message))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "No message specified."
				}, true);
				return;
			}

			if (Database.TryGetMessage(identifier.ToLower(), out Database.Message _))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "There is already a message with that identifier."
				}, true);
				return;
			}

			if(Database.AddMessage(identifier, command.Member.Id, message))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = "Message added."
				}, true);
			}
			else
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error: Failed adding the message to the database."
				}, true);
			}
		}
	}
}
