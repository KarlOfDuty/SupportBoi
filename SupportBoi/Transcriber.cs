using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DiscordChatExporter.Core.Models;
using DiscordChatExporter.Core.Services;
using DiscordChatExporter.Core.Services.Helpers;
using StyletIoC;

namespace SupportBoi
{
	internal static class Transcriber
	{
		internal static async Task<string> ExecuteAsync(string channelID, string ticketID)
		{
			// Get services
			var settingsService = TranscriberServices.Instance.Get<SettingsService>();
			var dataService = TranscriberServices.Instance.Get<DataService>();
			var exportService = TranscriberServices.Instance.Get<ExportService>();

			// Configure settings
			if (Config.timestampFormat != "")
				settingsService.DateFormat = Config.timestampFormat;

			// Get chat log
			var chatLog = await dataService.GetChatLogAsync(new AuthToken(AuthTokenType.Bot, Config.token), channelID);

			if (!Directory.Exists("./transcripts"))
			{
				Directory.CreateDirectory("./transcripts");
			}

			string fileName = "./transcripts/ticket-" + ticketID + ".html";

			// Export
			await exportService.ExportChatLogAsync(chatLog, fileName, ExportFormat.HtmlDark, 2000);

			return fileName;
		}
	}

	internal static class TranscriberServices
	{
		public static IContainer Instance { get; }

		static TranscriberServices()
		{
			var builder = new StyletIoCBuilder();

			// Autobind the .Services assembly
			builder.Autobind(typeof(DataService).Assembly);

			// Bind settings as singleton
			builder.Bind<SettingsService>().ToSelf().InSingletonScope();

			// Set instance
			Instance = builder.BuildContainer();
		}
	}
}
