using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using DSharpPlus.Entities;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
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

		private static Sheet CreateSheet(string staffID, string staffName)
		{
			// Create new sheet
			BatchUpdateSpreadsheetRequest createRequest = new BatchUpdateSpreadsheetRequest
			{
				Requests = new List<Request>
				{
					new Request
					{
						AddSheet = new AddSheetRequest { Properties = new SheetProperties { Title = staffName } }
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
								MetadataValue = staffID,
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
								MetadataValue = "ticketNumber",
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
								MetadataValue = "channel",
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
								MetadataValue = "user",
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
								MetadataValue = "timeCreated",
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
								MetadataValue = "lastMessage",
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
								MetadataValue = "summary",
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

		private static string ColumnIndexToLetter(int column)
		{
			const int offset = 26;
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

			string columnName = "";

			int tempNumber = column + 1;
			while (tempNumber > 0)
			{
				int position = tempNumber % offset;
				columnName = (position == 0 ? 'Z' : chars[position > 0 ? position - 1 : 0]) + columnName;
				tempNumber = (tempNumber - 1) / offset;
			}
			return columnName;
		}

		private static Dictionary<string, string> GetTicketColumnLetters(int sheetID)
		{
			var response = service.Spreadsheets.DeveloperMetadata.Search(new SearchDeveloperMetadataRequest()
			{
				DataFilters = new List<DataFilter>
				{
					new DataFilter
					{
						DeveloperMetadataLookup = new DeveloperMetadataLookup
						{
							LocationType = "COLUMN",
							MetadataKey = "TicketData",

						}
					}
				}
			}, Config.spreadsheetID).Execute();

			Dictionary<string, string> columnNames = new Dictionary<string, string>();

			foreach (MatchedDeveloperMetadata metadata in response.MatchedDeveloperMetadata)
			{
				bool isColumn = metadata.DeveloperMetadata.Location.DimensionRange.Dimension == "COLUMNS";
				bool isCorrectSheet = metadata.DeveloperMetadata.Location.DimensionRange.SheetId == sheetID;

				if (isColumn && isCorrectSheet)
				{
					columnNames.Add(metadata.DeveloperMetadata.MetadataValue, ColumnIndexToLetter(metadata.DeveloperMetadata.Location.DimensionRange.StartIndex ?? 0));
				}
			}

			if (!columnNames.ContainsKey("ticketNumber") || !columnNames.ContainsKey("channel") || !columnNames.ContainsKey("user") || !columnNames.ContainsKey("timeCreated") || !columnNames.ContainsKey("lastMessage") || !columnNames.ContainsKey("summary"))
			{
				return null;
			}

			return columnNames;
		}

		private static int GetTicketRow(Sheet sheet, uint ticketID)
		{
			Dictionary<string, string> columnLetters = GetTicketColumnLetters(sheet.Properties.SheetId ?? -1);

			if (columnLetters == null)
			{
				throw new ArgumentException("That ticket does not exist in the provided sheet. (" + sheet.Properties.Title + ")");
			}

			var request = service.Spreadsheets.Values.Get(Config.spreadsheetID,
				$"{sheet.Properties.Title}!{columnLetters["ticketNumber"]}:{columnLetters["ticketNumber"]}");
			ValueRange response = request.Execute();

			for (int i = 0; i < response.Values.Count; i++)
			{
				if (uint.TryParse(response.Values[i][0].ToString(), out uint value) && value == ticketID)
				{
					return i + 1;
				}
			}

			throw new ArgumentException("That ticket does not exist in the provided sheet. (" + sheet.Properties.Title + ")");
		}

		private static Tuple<Sheet, int> GetTicketRow(uint ticketID)
		{
			foreach (Sheet sheet in GetSpreadsheet().Sheets)
			{
				Dictionary<string, string> columnLetters = GetTicketColumnLetters(sheet.Properties.SheetId ?? -1);

				if (columnLetters == null)
				{
					continue;
				}
				
				var request = service.Spreadsheets.Values.Get(Config.spreadsheetID, $"{sheet.Properties.Title}!{columnLetters["ticketNumber"]}:{columnLetters["ticketNumber"]}");
				ValueRange response = request.Execute();

				for (int i = 0; i < response.Values.Count; i++)
				{
					if (uint.TryParse(response.Values[i][0].ToString(), out uint value) && value == ticketID)
					{
						return Tuple.Create(sheet, i + 1);
					}
				}
			}
			throw new ArgumentException("That ticket does not exist in the spreadsheet.");
		}

		private static void UpdateCell(Sheet sheet, string columnLetter, int rowNumber, string data, string url = null)
		{
			ValueRange valueRange = new ValueRange
			{
				MajorDimension = "COLUMNS",
				Values = new List<IList<object>>
				{
					new List<object> { url == null ? data : $"=HYPERLINK({url}, {data})" }
				}
			};

			SpreadsheetsResource.ValuesResource.UpdateRequest update = service.Spreadsheets.Values.Update(valueRange, Config.spreadsheetID, sheet.Properties.Title + "!" + columnLetter + rowNumber);
			update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
			update.Execute();
		}

		private static void AddMissingColumn()
		{
			throw new NotImplementedException();
		}

		private static int GetNextEmptyRow(Sheet sheet)
		{
			Dictionary<string, string> columnLetters = GetTicketColumnLetters(sheet.Properties.SheetId ?? -1);

			var request = service.Spreadsheets.Values.BatchGet(Config.spreadsheetID);
			request.Ranges = new []
			{
				$"{sheet.Properties.Title}!{columnLetters["ticketNumber"]}:{columnLetters["ticketNumber"]}",
				$"{sheet.Properties.Title}!{columnLetters["channel"]}:{columnLetters["channel"]}",
				$"{sheet.Properties.Title}!{columnLetters["user"]}:{columnLetters["user"]}",
				$"{sheet.Properties.Title}!{columnLetters["timeCreated"]}:{columnLetters["timeCreated"]}",
				$"{sheet.Properties.Title}!{columnLetters["lastMessage"]}:{columnLetters["lastMessage"]}",
				$"{sheet.Properties.Title}!{columnLetters["summary"]}:{columnLetters["summary"]}"
			};
			request.MajorDimension = SpreadsheetsResource.ValuesResource.BatchGetRequest.MajorDimensionEnum.ROWS;
			BatchGetValuesResponse response = request.Execute();

			return response.ValueRanges.Select(x => x.Values.Count).ToArray().Max() + 1;
		}

		private static Sheet GetOrCreateSheet(string staffID, string staffName = "Unknown staff member")
		{
			Spreadsheet spreadsheet = GetSpreadsheet();
			if (spreadsheet == null)
			{
				throw new NullReferenceException("Google API returned null when attempting to find spreadsheet, is the ID correct?");
			}

			try
			{
				// Checks for a sheet which has a staff id corresponding to this staff member in it's metadata
				return spreadsheet.Sheets.First(s => s?.DeveloperMetadata?.Any(m => m?.MetadataKey == "StaffID" && m.MetadataValue == (staffID ?? "0")) ?? false);
			}
			catch (Exception)
			{
				// Creates a new sheet if the target one does not exist
				return CreateSheet(staffID, staffID == null ? "Unassigned" : staffName);
			}
		}

		public static void AddTicket(DiscordMember user, DiscordChannel channel, string ticketNumber, string staffID = null, string staffName = null)
		{
			if (!Config.sheetsEnabled)
			{
				return;
			}

			Sheet sheet = GetOrCreateSheet(staffID, staffName);

			Dictionary<string, string> columnLetters = GetTicketColumnLetters(sheet.Properties.SheetId ?? -1);

			int nextRow = GetNextEmptyRow(sheet);

			UpdateCell(sheet, columnLetters["ticketNumber"], nextRow, ticketNumber);
			UpdateCell(sheet, columnLetters["channel"], nextRow, $"\"#{channel.Name}\"", $"\"https://discordapp.com/channels/{channel.GuildId}/{channel.Id}/\"");
			UpdateCell(sheet, columnLetters["user"], nextRow, user.Nickname == null ? $"\"{user.Username}#{user.Discriminator}\"" : $"\"{user.DisplayName} ({user.Username}#{user.Discriminator})\"", $"\"https://discordapp.com/channels/@me/{user.Id}\"");
			UpdateCell(sheet, columnLetters["timeCreated"], nextRow, DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
			UpdateCell(sheet, columnLetters["lastMessage"], nextRow, DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
			UpdateCell(sheet, columnLetters["summary"], nextRow, "No summary yet. Use '" + Config.prefix + "setsummary' to edit it.");
		}

		public static void SetSummary(uint ticketID, ulong staffID, string summary)
		{
			if (!Config.sheetsEnabled)
			{
				return;
			}

			Sheet sheet = GetOrCreateSheet(staffID.ToString());

			Dictionary<string, string> columnLetters = GetTicketColumnLetters(sheet.Properties.SheetId ?? -1);
			
			UpdateCell(sheet, columnLetters["summary"], GetTicketRow(sheet, ticketID), $"{summary}");
		}

		public static void RefreshLastMessageSent()
		{
			throw new NotImplementedException();
		}

		public static void DeleteTicket(uint ticketID)
		{
			GetTicketRow(ticketID).Deconstruct(out Sheet sheet, out int ticketRow);

			BatchUpdateSpreadsheetRequest request = new BatchUpdateSpreadsheetRequest
			{
				Requests = new List<Request>
				{
					new Request
					{
						DeleteDimension = new DeleteDimensionRequest
						{
							Range = new DimensionRange
							{
								SheetId = sheet.Properties.SheetId,
								Dimension = "ROWS",
								StartIndex = ticketRow - 1,
								EndIndex = ticketRow
							}
						}
					}
				}
			};
			service.Spreadsheets.BatchUpdate(request, Config.spreadsheetID).Execute();
		}

		public static void AssignTicket()
		{
			throw new NotImplementedException();
		}
	}
}
