﻿using System.IO;
using System.Threading.Tasks;

using DiscordChatExporter.Core.Discord;
using DiscordChatExporter.Core.Discord.Data;
using DiscordChatExporter.Core.Exporting;
using DiscordChatExporter.Core.Exporting.Filtering;
using DiscordChatExporter.Core.Exporting.Partitioning;

namespace SupportBoi;

internal static class Transcriber
{
    private static string transcriptDir = "./transcripts";

    internal static async Task ExecuteAsync(ulong channelID, uint ticketID)
    {
        DiscordClient discordClient = new DiscordClient(Config.token);
        ChannelExporter exporter = new ChannelExporter(discordClient);

        if (!string.IsNullOrEmpty(SupportBoi.commandLineArgs.transcriptDir))
        {
            transcriptDir = SupportBoi.commandLineArgs.transcriptDir;
        }

        if (!Directory.Exists(transcriptDir))
        {
            Directory.CreateDirectory(transcriptDir);
        }

        Channel channel = await discordClient.GetChannelAsync(new Snowflake(channelID));
        Guild guild = await discordClient.GetGuildAsync(channel.GuildId);

        ExportRequest request = new (
            guild,
            channel,
            GetPath(ticketID),
            null,
            ExportFormat.HtmlDark,
            null,
            null,
            PartitionLimit.Null,
            MessageFilter.Null,
            true,
            false,
            false,
            "en-SE",
            true
        );

        await exporter.ExportChannelAsync(request);
    }

    internal static string GetPath(uint ticketNumber)
    {
        return transcriptDir + "/" + GetFilename(ticketNumber);
    }

    internal static string GetFilename(uint ticketNumber)
    {
        return "ticket-" + ticketNumber.ToString("00000") + ".html";
    }
}