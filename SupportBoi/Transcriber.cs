using System.IO;
using System.Threading.Tasks;

using DiscordChatExporter.Core.Discord;
using DiscordChatExporter.Core.Discord.Data;
using DiscordChatExporter.Core.Exceptions;
using DiscordChatExporter.Core.Exporting;
using DiscordChatExporter.Core.Exporting.Filtering;
using DiscordChatExporter.Core.Exporting.Partitioning;
using DiscordChatExporter.Core.Utils.Extensions;

namespace SupportBoi
{
	internal static class Transcriber
	{
		internal static async Task ExecuteAsync(ulong channelID, uint ticketID)
		{
			DiscordClient discordClient = new DiscordClient(new AuthToken(AuthTokenKind.Bot, Config.token));
			ChannelExporter Exporter = new ChannelExporter(discordClient);

			if (!Directory.Exists("./transcripts"))
			{
				Directory.CreateDirectory("./transcripts");
			}

			string dateFormat = "yyyy-MMM-dd HH:mm";

			// Configure settings
			if (Config.timestampFormat != "")
				dateFormat = Config.timestampFormat;

			Channel channel = await discordClient.GetChannelAsync(new Snowflake(channelID));
			Guild guild = await discordClient.GetGuildAsync(channel.GuildId);

			ExportRequest request = new ExportRequest(
				guild: guild,
				channel: channel,
				outputPath: GetPath(ticketID),
				format: ExportFormat.HtmlDark,
				after: null,
				before: null,
				partitionLimit: PartitionLimit.Null,
				messageFilter: MessageFilter.Null,
				shouldDownloadMedia: false,
				shouldReuseMedia: false,
				dateFormat: dateFormat
			);

			await Exporter.ExportChannelAsync(request);
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
}
