using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
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

		public static async Task<List<Database.Category>> GetVerifiedChannels()
		{
			List<Database.Category> verifiedCategories = new List<Database.Category>();
			foreach (Database.Category category in Database.GetAllCategories())
			{
				DiscordChannel channel = null;
				try
				{
					channel = await SupportBoi.discordClient.GetChannelAsync(category.id);
				}
				catch (Exception) { /*ignored*/ }

				if (channel != null)
				{
					verifiedCategories.Add(category);
				}
				else
				{
					Logger.Warn("Category '" + category.name + "' (" + category.id + ") no longer exists! Ignoring...");
				}
			}
			return verifiedCategories;
		}
	}
}
