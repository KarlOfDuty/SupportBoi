using System.IO;
using System.Threading.Tasks;
using DiscordChatExporter.Core.Models;
using DiscordChatExporter.Core.Services;
using StyletIoC;

namespace SupportBoi
{
	internal static class Transcriber
	{
		internal static async Task ExecuteAsync(string channelID, uint ticketID)
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

			// Export
			await exportService.ExportChatLogAsync(chatLog, GetPath(ticketID), ExportFormat.HtmlDark, 2000);
		}

		internal static string GetPath(uint ticketNumber)
		{
			return "./transcripts/ticket-" + ticketNumber.ToString("00000") + ".html";
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
