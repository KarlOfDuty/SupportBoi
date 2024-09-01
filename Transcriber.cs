using System.IO;
using System.Threading.Tasks;

using DiscordChatExporter.Core.Discord;
using DiscordChatExporter.Core.Discord.Data;
using DiscordChatExporter.Core.Exporting;
using DiscordChatExporter.Core.Exporting.Filtering;
using DiscordChatExporter.Core.Exporting.Partitioning;

namespace SupportBoi;

internal static class Transcriber
{
    internal static async Task ExecuteAsync(ulong channelID, uint ticketID)
    {
        DiscordClient discordClient = new DiscordClient(Config.token);
        ChannelExporter exporter = new ChannelExporter(discordClient);

        if (!Directory.Exists("./transcripts"))
        {
            Directory.CreateDirectory("./transcripts");
        }

        Channel channel = await discordClient.GetChannelAsync(new Snowflake(channelID));
        Guild guild = await discordClient.GetGuildAsync(channel.GuildId);

        ExportRequest request = new ExportRequest(
            Guild: guild,
            Channel: channel,
            OutputPath: GetPath(ticketID),
            Format: ExportFormat.HtmlDark,
            After: null,
            Before: null,
            PartitionLimit: PartitionLimit.Null,
            MessageFilter: MessageFilter.Null,
            ShouldDownloadMedia: false,
            ShouldReuseMedia: false,
            DateFormat: "yyyy-MMM-dd HH:mm"
            );

        await exporter.ExportChannelAsync(request);
    }

    internal static string GetPath(uint ticketNumber)
    {
        return "./transcripts/" + GetFilename(ticketNumber);
    }

    internal static string GetFilename(uint ticketNumber)
    {
        return "ticket-" + ticketNumber.ToString("00000") + ".html";
    }
}