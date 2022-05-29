using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace SupportBoi
{
	public enum LogID
	{
		GENERAL,
		CONFIG,
		GITHUB,
		COMMAND,
		DISCORD,
		DATABASE
	};

	public static class Logger
	{
		private static Dictionary<LogID, EventId> eventIDs = new Dictionary<LogID, EventId>
		{
			{ LogID.GENERAL,  new EventId(500, "General")  },
			{ LogID.CONFIG,   new EventId(501, "Config")   },
			{ LogID.GITHUB,   new EventId(502, "Github")   },
			{ LogID.COMMAND,  new EventId(503, "Command")  },
			{ LogID.DISCORD,  new EventId(504, "Discord")  },
			{ LogID.DATABASE, new EventId(505, "Database") },
		};

		public static void Debug(LogID logID, string Message)
		{
			try
			{
				SupportBoi.discordClient.Logger.Log(LogLevel.Debug, eventIDs[logID], Message);
			}
			catch (NullReferenceException)
			{
				Console.WriteLine("[DEBUG] " + Message);
			}
		}

		public static void Log(LogID logID, string Message)
		{
			try
			{
				SupportBoi.discordClient.Logger.Log(LogLevel.Information, eventIDs[logID], Message);
			}
			catch (NullReferenceException)
			{
				Console.WriteLine("[INFO] " + Message);
			}
		}

		public static void Warn(LogID logID, string Message)
		{
			try
			{
				SupportBoi.discordClient.Logger.Log(LogLevel.Warning, eventIDs[logID], Message);
			}
			catch (NullReferenceException)
			{
				Console.WriteLine("[WARNING] " + Message);
			}
		}

		public static void Error(LogID logID, string Message)
		{
			try
			{
				SupportBoi.discordClient.Logger.Log(LogLevel.Error, eventIDs[logID], Message);
			}
			catch (NullReferenceException)
			{
				Console.WriteLine("[ERROR] " + Message);
			}
		}

		public static void Fatal(LogID logID, string Message)
		{
			try
			{
				SupportBoi.discordClient.Logger.Log(LogLevel.Critical, eventIDs[logID], Message);
			}
			catch (NullReferenceException)
			{
				Console.WriteLine("[CRITICAL] " + Message);
			}
		}
	}
}