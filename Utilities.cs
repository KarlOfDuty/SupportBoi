using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace SupportBoi;

public static class Utilities
{
    private static readonly Random rng = new Random();

    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
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