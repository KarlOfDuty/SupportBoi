using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using Newtonsoft.Json;

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
				credential = GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.Load(stream).Secrets, scopes, "SupportBoi", CancellationToken.None, new FileDataStore("token.json", true)).Result;
				Console.WriteLine("Google credential file saved to 'token.json'");
			}

			// Create Google Sheets API service.
			service = new SheetsService(new BaseClientService.Initializer()
			{
				HttpClientInitializer = credential,
				ApplicationName = "SupportBoi"
			});

			SpreadsheetsResource.GetRequest request = service.Spreadsheets.Get(Config.spreadsheetID);

			Spreadsheet spreadsheet = request.Execute();

			if (spreadsheet == null)
			{
				Console.WriteLine();
				throw new ArgumentException("ERROR: Could not find a Google Sheets Spreadsheet with provided ID.");
			}
		}

		private static Spreadsheet GetSpreadsheet()
		{
			SpreadsheetsResource.GetRequest request = service.Spreadsheets.Get(Config.spreadsheetID);

			return request.Execute();
		}

		private static Sheet CreateSheet(string staffName, string staffID)
		{
			// Create new sheet
			BatchUpdateSpreadsheetRequest createRequest = new BatchUpdateSpreadsheetRequest
			{
				Requests = new List<Request>
				{
					new Request
					{
						AddSheet = new AddSheetRequest { Properties = new SheetProperties { Title = staffName ?? "Unassigned" } }
					}
				}
			};
			BatchUpdateSpreadsheetResponse createResponse = service.Spreadsheets.BatchUpdate(createRequest, Config.spreadsheetID).Execute();

			// Add metadata to the table specifying which staff member the table belongs to
			int sheetID = createResponse.Replies.FirstOrDefault()?.AddSheet.Properties.SheetId ?? -1;
			BatchUpdateSpreadsheetRequest metadataRequest = new BatchUpdateSpreadsheetRequest
			{
				Requests = new List<Request>
				{
					new Request
					{
						CreateDeveloperMetadata = new CreateDeveloperMetadataRequest
						{
							DeveloperMetadata = new DeveloperMetadata
							{
								Visibility = "document",
								MetadataKey = "StaffID",
								MetadataValue = staffID ?? "0",
								Location = new DeveloperMetadataLocation { SheetId = sheetID }
							}
						}
					},
					new Request
					{
						AppendCells = new AppendCellsRequest
						{
							Rows = new List<RowData>
							{
								new RowData
								{
									Values = new List<CellData>
									{
										new CellData
										{
											UserEnteredValue = new ExtendedValue
											{
												StringValue = "Ticket Number"
											}
										},
										new CellData
										{
											UserEnteredValue = new ExtendedValue
											{
												StringValue = "Channel"
											}
										},
										new CellData
										{
											UserEnteredValue = new ExtendedValue
											{
												StringValue = "User"
											}
										},
										new CellData
										{
											UserEnteredValue = new ExtendedValue
											{
												StringValue = "Time created"
											}
										},
										new CellData
										{
											UserEnteredValue = new ExtendedValue
											{
												StringValue = "Last Message"
											}
										},
										new CellData
										{
											UserEnteredValue = new ExtendedValue
											{
												StringValue = "Summary"
											}
										}
									}
								}
							},
							SheetId = sheetID,
							Fields = "*"
						}
					},
					new Request
					{
						CreateDeveloperMetadata = new CreateDeveloperMetadataRequest
						{
							DeveloperMetadata = new DeveloperMetadata
							{
								Visibility = "document",
								MetadataKey = "TicketData",
								MetadataValue = "Ticket Number",
								Location = new DeveloperMetadataLocation
								{
									DimensionRange = new DimensionRange
									{
										Dimension = "COLUMNS",
										StartIndex = 0,
										EndIndex = 1,
										SheetId = sheetID
									}
								}
							}
						}
					},
					new Request
					{
						CreateDeveloperMetadata = new CreateDeveloperMetadataRequest
						{
							DeveloperMetadata = new DeveloperMetadata
							{
								Visibility = "document",
								MetadataKey = "TicketData",
								MetadataValue = "Channel",
								Location = new DeveloperMetadataLocation
								{
									DimensionRange = new DimensionRange
									{
										Dimension = "COLUMNS",
										StartIndex = 1,
										EndIndex = 2,
										SheetId = sheetID
									}
								}
							}
						}
					},
					new Request
					{
						CreateDeveloperMetadata = new CreateDeveloperMetadataRequest
						{
							DeveloperMetadata = new DeveloperMetadata
							{
								Visibility = "document",
								MetadataKey = "TicketData",
								MetadataValue = "User",
								Location = new DeveloperMetadataLocation
								{
									DimensionRange = new DimensionRange
									{
										Dimension = "COLUMNS",
										StartIndex = 2,
										EndIndex = 3,
										SheetId = sheetID
									}
								}
							}
						}
					},
					new Request
					{
						CreateDeveloperMetadata = new CreateDeveloperMetadataRequest
						{
							DeveloperMetadata = new DeveloperMetadata
							{
								Visibility = "document",
								MetadataKey = "TicketData",
								MetadataValue = "Time created",
								Location = new DeveloperMetadataLocation
								{
									DimensionRange = new DimensionRange
									{
										Dimension = "COLUMNS",
										StartIndex = 3,
										EndIndex = 4,
										SheetId = sheetID
									}
								}
							}
						}
					},
					new Request
					{
						CreateDeveloperMetadata = new CreateDeveloperMetadataRequest
						{
							DeveloperMetadata = new DeveloperMetadata
							{
								Visibility = "document",
								MetadataKey = "TicketData",
								MetadataValue = "Last Message",
								Location = new DeveloperMetadataLocation
								{
									DimensionRange = new DimensionRange
									{
										Dimension = "COLUMNS",
										StartIndex = 4,
										EndIndex = 5,
										SheetId = sheetID
									}
								}
							}
						}
					},
					new Request
					{
						CreateDeveloperMetadata = new CreateDeveloperMetadataRequest
						{
							DeveloperMetadata = new DeveloperMetadata
							{
								Visibility = "document",
								MetadataKey = "TicketData",
								MetadataValue = "Summary",
								Location = new DeveloperMetadataLocation
								{
									DimensionRange = new DimensionRange
									{
										Dimension = "COLUMNS",
										StartIndex = 5,
										EndIndex = 6,
										SheetId = sheetID
									}
								}
							}
						}
					},
				}
			};
			service.Spreadsheets.BatchUpdate(metadataRequest, Config.spreadsheetID).Execute();
			


			return GetSpreadsheet().Sheets.FirstOrDefault(s => s.Properties.SheetId == sheetID);
		}

		public static bool AddTicket(CommandContext command, string channelID, string ticketNumber, string staffID = null, string staffName = null)
		{
			Spreadsheet spreadsheet = GetSpreadsheet();
			if (spreadsheet == null)
			{
				return false;
			}

			Sheet sheet;
			try
			{
				// Checks for a sheet which has a staff id corresponding to this staff member in it's metadata
				sheet = spreadsheet.Sheets.First(s => s?.DeveloperMetadata?.Any(m => m?.MetadataKey == "StaffID" && m?.MetadataValue == (staffID ?? "0")) ?? false);
			}
			catch (Exception)
			{
				// Creates a new sheet if the target one does not exist
				sheet = CreateSheet(staffName, staffID);
			}
			return true;
		}

		//public static bool RefreshLastMessageSent()
		//{

		//}

		//public static bool RemoveTicket()
		//{

		//}

		//public static bool AssignTicket()
		//{

		//}
	}
}
