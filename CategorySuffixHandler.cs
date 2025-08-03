using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace SupportBoi;

public static partial class CategorySuffixHandler
{
    private const int RETRY_INTERVAL_MIN = 10;
    private const int CHECK_INTERVAL_SEC = 30;
    private static ConcurrentDictionary<ulong, DateTime> nextAllowedRetries = new();
    private static Utilities.ConcurrentSet<ulong> retryQueue = new();
    private static readonly CancellationTokenSource retryTaskToken = new();

    public static void Start()
    {
        Logger.Log("Starting category suffix handler.");
        Task.Run(() => Run(retryTaskToken.Token));
    }

    private static async Task Run(CancellationToken token)
    {
        PeriodicTimer timer = new(TimeSpan.FromSeconds(CHECK_INTERVAL_SEC));

        try
        {
            while (await timer.WaitForNextTickAsync(token))
            {
                List<ulong> retryQueueCopy = new(retryQueue.Items);

                // Loop over a copy of the set for thread safety
                foreach (ulong categoryID in retryQueueCopy)
                {
                    if (!IsRetryAllowed(categoryID))
                    {
                        continue;
                    }

                    try
                    {
                        retryQueue.Remove(categoryID);
                        await UpdateCategorySuffix(categoryID);
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Retrying category update failed for '" + categoryID + "'", e);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            Logger.Log("Shutting down category suffix handler.");
        }
    }

    [GeneratedRegex(@" \(\d+\)$")]
    private static partial Regex CategorySuffixPattern();

    private static string ReplaceSuffix(string original, int number)
    {
        return CategorySuffixPattern().Replace(original, "").TrimEnd() + " (" + number + ")";
    }

    private static bool IsRetryAllowed(ulong categoryID)
    {
        return !nextAllowedRetries.TryGetValue(categoryID, out DateTime timeout) || timeout < DateTime.UtcNow;
    }

    public static async Task ScheduleSuffixUpdate(ulong categoryID)
    {
        if (!Config.addCategoryTicketCount || (!Database.Category.TryGetCategory(categoryID, out _) && Config.ticketCountRegisteredOnly))
        {
            return;
        }

        if (IsRetryAllowed(categoryID))
        {
            nextAllowedRetries[categoryID] = DateTime.UtcNow.AddMinutes(RETRY_INTERVAL_MIN);
            if (retryQueue.Contains(categoryID))
            {
                retryQueue.Remove(categoryID);
            }
            await UpdateCategorySuffix(categoryID);
        }
        else
        {
            if (!retryQueue.Contains(categoryID))
            {
                retryQueue.Add(categoryID);
            }
        }
    }

    private static async Task UpdateCategorySuffix(ulong categoryID)
    {
        try
        {
            DiscordChannel category;
            try
            {
                category = await SupportBoi.client.GetChannelAsync(categoryID);
            }
            catch (Exception e)
            {
                Logger.Error("UpdateCategorySuffix: Unable to get category.", e);
                return;
            }

            if (!category.IsCategory)
            {
                Logger.Error("UpdateCategorySuffix: Channel is not a category.");
                return;
            }

            if (!Database.Ticket.TryGetOpenTickets(out List<Database.Ticket> tickets))
            {
                await category.ModifyAsync(x => x.Name = ReplaceSuffix(category.Name, 0));
                return;
            }

            HashSet<ulong> openTicketChannelIDs = tickets.Select(t => t.channelID).ToHashSet();
            int numberOfTickets = category.Children.Count(c => openTicketChannelIDs.Contains(c.Id));

            Logger.Debug("Updating category name to: " + ReplaceSuffix(category.Name, numberOfTickets));
            try
            {
                await category.ModifyAsync(x => x.Name = ReplaceSuffix(category.Name, numberOfTickets));
            }
            catch (DSharpPlus.Exceptions.RateLimitException)
            {
                if (!retryQueue.Contains(categoryID))
                {
                    retryQueue.Add(categoryID);
                }
                Logger.Warn("Rate limit error while updating category suffix.");
            }
        }
        catch (Exception e)
        {
            Logger.Error("Error occurred while adding suffix to category name.", e);
        }
    }
}