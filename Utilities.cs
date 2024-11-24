using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace SupportBoi;

public static class Extensions
{
    public static bool ContainsAny(this string haystack, params string[] needles)
    {
        return needles.Any(haystack.Contains);
    }

    public static bool ContainsAny(this string haystack, params char[] needles)
    {
        return needles.Any(haystack.Contains);
    }
}

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
                channel = await SupportBoi.client.GetChannelAsync(category.id);
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

    public static string ReadManifestData(string embeddedFileName)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string resourceName = assembly.GetManifestResourceNames().First(s => s.EndsWith(embeddedFileName,StringComparison.CurrentCultureIgnoreCase));

        using Stream stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException("Could not load manifest resource stream.");
        }

        using StreamReader reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static DiscordColor StringToColor(string color)
    {
        switch (color.ToLower())
        {
            case "black":
                return DiscordColor.Black;
            case "white":
                return DiscordColor.White;
            case "gray":
                return DiscordColor.Gray;
            case "darkgray":
                return DiscordColor.DarkGray;
            case "lightgray":
                return DiscordColor.LightGray;
            case "verydarkgray":
                return DiscordColor.VeryDarkGray;
            case "blurple":
                return DiscordColor.Blurple;
            case "grayple":
                return DiscordColor.Grayple;
            case "darkbutnotblack":
                return DiscordColor.DarkButNotBlack;
            case "notquiteblack":
                return DiscordColor.NotQuiteBlack;
            case "red":
                return DiscordColor.Red;
            case "darkred":
                return DiscordColor.DarkRed;
            case "green":
                return DiscordColor.Green;
            case "darkgreen":
                return DiscordColor.DarkGreen;
            case "blue":
                return DiscordColor.Blue;
            case "darkblue":
                return DiscordColor.DarkBlue;
            case "yellow":
                return DiscordColor.Yellow;
            case "cyan":
                return DiscordColor.Cyan;
            case "magenta":
                return DiscordColor.Magenta;
            case "teal":
                return DiscordColor.Teal;
            case "aquamarine":
                return DiscordColor.Aquamarine;
            case "gold":
                return DiscordColor.Gold;
            case "goldenrod":
                return DiscordColor.Goldenrod;
            case "azure":
                return DiscordColor.Azure;
            case "rose":
                return DiscordColor.Rose;
            case "springgreen":
                return DiscordColor.SpringGreen;
            case "chartreuse":
                return DiscordColor.Chartreuse;
            case "orange":
                return DiscordColor.Orange;
            case "purple":
                return DiscordColor.Purple;
            case "violet":
                return DiscordColor.Violet;
            case "brown":
                return DiscordColor.Brown;
            case "hotpink":
                return DiscordColor.HotPink;
            case "lilac":
                return DiscordColor.Lilac;
            case "cornflowerblue":
                return DiscordColor.CornflowerBlue;
            case "midnightblue":
                return DiscordColor.MidnightBlue;
            case "wheat":
                return DiscordColor.Wheat;
            case "indianred":
                return DiscordColor.IndianRed;
            case "turquoise":
                return DiscordColor.Turquoise;
            case "sapgreen":
                return DiscordColor.SapGreen;
            case "phthaloblue":
                return DiscordColor.PhthaloBlue;
            case "phthalogreen":
                return DiscordColor.PhthaloGreen;
            case "sienna":
                return DiscordColor.Sienna;
            default:
                try
                {
                    return new DiscordColor(color);
                }
                catch (Exception)
                {
                    return DiscordColor.None;
                }
        }
    }
}