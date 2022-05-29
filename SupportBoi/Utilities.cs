using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

namespace SupportBoi
{
	public static class Utilities
	{
		public static List<T> RandomizeList<T>(List<T> list)
		{
			RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
			int n = list.Count;
			while (n > 1)
			{
				byte[] box = new byte[1];
				do provider.GetBytes(box);
				while (!(box[0] < n * (Byte.MaxValue / n)));
				int k = (box[0] % n);
				n--;
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}

			return list;
		}

		public static string[] ParseIDs(string args)
		{
			if (string.IsNullOrEmpty(args))
			{
				return new string[0];
			}
			return  args.Trim().Replace("<@!", "").Replace("<@", "").Replace(">", "").Split();
		}

		public static LinkedList<string> ParseListIntoMessages(List<string> listItems)
		{ 
			LinkedList<string> messages = new LinkedList<string>();

			foreach (string listItem in listItems)
			{
				if (messages.Last?.Value?.Length + listItem?.Length < 2048)
				{
					messages.Last.Value += listItem;
				}
				else
				{
					messages.AddLast(listItem);
				}
			}

			return messages;
		}

		public static DiscordRole GetRoleByName(DiscordGuild guild, string Name)
		{
			Name = Name.Trim().ToLower();
			foreach (DiscordRole role in guild.Roles.Values)
			{
				if (role.Name.ToLower().StartsWith(Name))
				{
					return role;
				}
			}

			return null;
		}
		
		public static async Task<bool> VerifyPermission(CommandContext command, string permission)
		{
			try
			{
				// Check if the user has permission to use this command.
				if (!Config.HasPermission(command.Member, permission))
				{
					await command.RespondAsync(new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "You do not have permission to use this command."
					});
					return false;
				}

				return true;
			}
			catch (Exception)
			{
				await command.RespondAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error occured when checking permissions, please report this to the developer."
				});
				return false;
			}
		}
	}
}
