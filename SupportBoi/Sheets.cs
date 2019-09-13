using System;
using System.IO;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;

namespace SupportBoi
{
	public static class Sheets
	{
		private static readonly string[] scopes = { SheetsService.Scope.Spreadsheets };

		private static UserCredential credential;

		private static SheetsService service;

		public static void Reload()
		{
			service?.Dispose();
			credential = null;

			if (!Config.sheetsEnabled)
			{
				return;
			}

			using (FileStream stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
			{
				credential = GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.Load(stream).Secrets, scopes, "SupportBoi", CancellationToken.None, new FileDataStore("./token.json", true)).Result;
				Console.WriteLine("Google credential file saved to ./token.json");
			}

			// Create Google Sheets API service.
			service = new SheetsService(new BaseClientService.Initializer()
			{
				HttpClientInitializer = credential,
				ApplicationName = "SupportBoi"
			});


		}
	}
}
