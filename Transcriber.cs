using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

using DiscordChatExporter.Core.Discord;
using DiscordChatExporter.Core.Discord.Data;
using DiscordChatExporter.Core.Exporting;
using DiscordChatExporter.Core.Exporting.Filtering;
using DiscordChatExporter.Core.Exporting.Partitioning;

namespace SupportBoi;

internal static class Transcriber
{
    private static string transcriptDir = "./transcripts"; // TODO: Should be local variable (or come from the config class) when added to config to avoid race conditions when reloading

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

        string htmlPath = GetHtmlPath(ticketID);
        string zipPath = GetZipPath(ticketID);
        string assetDirPath = GetAssetDirPath(ticketID);

        string assetDirName = GetAssetDirName(ticketID);
        string htmlFilename = GetHTMLFilename(ticketID);
        
        if (File.Exists(htmlPath))
        {
            File.Delete(htmlPath);
        }

        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        if (Directory.Exists(assetDirPath))
        {
            Directory.Delete(assetDirPath, true);
        }

        Channel channel = await discordClient.GetChannelAsync(new Snowflake(channelID));
        Guild guild = await discordClient.GetGuildAsync(channel.GuildId);

        ExportRequest request = new (
            guild,
            channel,
            htmlPath,
            assetDirPath,
            ExportFormat.HtmlDark,
            null,
            null,
            PartitionLimit.Null,
            MessageFilter.Null,
            true,
            true,
            true,
            "en-SE",
            true
        );

        await exporter.ExportChannelAsync(request);

        string[] assetFiles;
        try
        {
            assetFiles = Directory.GetFiles(assetDirPath);
        }
        catch (Exception)
        {
            assetFiles = [];
        }

        if (assetFiles.Length > 0)
        {
            using (ZipArchive zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                zip.CreateEntryFromFile(htmlPath, htmlFilename);
                foreach (string assetFile in assetFiles)
                {
                    zip.CreateEntryFromFile(assetFile, assetDirName + "/" + Path.GetFileName(assetFile));
                }
            }

            Directory.Delete(assetDirPath, true);
        }
    }

    internal static string GetHtmlPath(uint ticketNumber)
    {
        return transcriptDir + "/" + GetHTMLFilename(ticketNumber);
    }

    internal static string GetZipPath(uint ticketNumber)
    {
        return transcriptDir + "/" + GetZipFilename(ticketNumber);
    }

    internal static string GetAssetDirPath(uint ticketNumber)
    {
        return transcriptDir + "/" + GetAssetDirName(ticketNumber);
    }

    internal static string GetAssetDirName(uint ticketNumber)
    {
        return "ticket-" + ticketNumber.ToString("00000") + "-assets";
    }

    internal static string GetHTMLFilename(uint ticketNumber)
    {
        return "ticket-" + ticketNumber.ToString("00000") + ".html";
    }

    internal static string GetZipFilename(uint ticketNumber)
    {
        return "ticket-" + ticketNumber.ToString("00000") + ".zip";
    }
}