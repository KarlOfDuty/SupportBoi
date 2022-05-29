using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SupportBoi.Commands
{
	public class SayCommand : BaseCommandModule
	{
		[Command("say")]
		[Cooldown(1, 2, CooldownBucketType.Channel)]
		[Description("Prints a message with information from staff.")]
		public async Task OnExecute(CommandContext command, string identifier)
		{
			if (!await Utilities.VerifyPermission(command, "say")) return;

			if (!Database.TryGetMessage(identifier.ToLower(), out Database.Message message))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "There is no message with that identifier."
				};
				await command.RespondAsync(error);
				return;
			}

			DiscordEmbed reply = new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = message.message
			};
			await command.RespondAsync(reply);
		}

		[Command("say")]
		[Cooldown(1, 2.0, CooldownBucketType.Channel)]
		[Description("Prints a list of staff messages.")]
		public async Task OnExecute(CommandContext command)
		{
			// Check if the user has permission to use this command.
			if (!Config.HasPermission(command.Member, "say"))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "You do not have permission to use this command."
				};
				await command.RespondAsync(error);
				command.Client.Logger.Log(LogLevel.Information, "User tried to use the say command but did not have permission.");
				return;
			}


			List<Database.Message> messages = Database.GetAllMessages();
			if (!messages.Any())
			{
				DiscordEmbed error = new DiscordEmbedBuilder()
					.WithColor(DiscordColor.Red)
					.WithDescription("There are no messages registered.");
				await command.RespondAsync(error);
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
				DiscordEmbed channelInfo = new DiscordEmbedBuilder()
					.WithTitle("Available messages: ")
					.WithColor(DiscordColor.Green)
					.WithDescription(listMessage);
				await command.RespondAsync(channelInfo);
			}
		}
	}
}
