using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SupportBoi.Commands
{
	public class SayCommand : ApplicationCommandModule
	{
		[SlashRequireGuild]
		[Config.ConfigPermissionCheckAttribute("say")]
		[SlashCommand("say", "Prints a message with information from staff. Use without identifier to list all identifiers.")]
		public async Task OnExecute(InteractionContext command, [Option("Identifier", "(Optional) The identifier word to summon a message.")] string identifier = null)
		{
			// Print list of all messages if no identifier is provided
			if (identifier == null)
			{
				List<Database.Message> messages = Database.GetAllMessages();
				if (!messages.Any())
				{
					await command.CreateResponseAsync(new DiscordEmbedBuilder()
					{
						Color = DiscordColor.Red,
						Description = "There are no messages registered."
					}, true);
					return;
				}

				List<string> listItems = new List<string>();
				foreach (Database.Message message in messages)
				{
					listItems.Add("**" + message.identifier + "** Added by <@" + message.userID + ">\n");
				}

				LinkedList<string> listMessages = Utilities.ParseListIntoMessages(listItems);
				foreach (string listMessage in listMessages)
				{
					await command.CreateResponseAsync(new DiscordEmbedBuilder()
					{
						Title = "Available messages: ",
						Color = DiscordColor.Green,
						Description = listMessage
					}, true);
				}
			}
			// Print specific message
			else
			{
				if (!Database.TryGetMessage(identifier.ToLower(), out Database.Message message))
				{
					await command.CreateResponseAsync(new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "There is no message with that identifier."
					}, true);
					return;
				}
				
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Cyan,
					Description = message.message.Replace("\\n", "\n")
				});				
			}
		}
	}
}
