using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SupportBoi
{
	public static class Logger
	{
		public static void Debug(string Message)
		{
			try
			{
				SupportBoi.discordClient.Logger.Log(LogLevel.Debug, new EventId(420, Assembly.GetEntryAssembly()?.GetName().Name), Message);
			}
			catch (NullReferenceException)
			{
				Console.WriteLine("[DEBUG] " + Message);
			}
		}

		public static void Log(string Message)
		{
			try
			{
				SupportBoi.discordClient.Logger.Log(LogLevel.Information, new EventId(420, Assembly.GetEntryAssembly()?.GetName().Name), Message);
			}
			catch (NullReferenceException)
			{
				Console.WriteLine("[INFO] " + Message);
			}
		}

		public static void Warn(string Message)
		{
			try
			{
				SupportBoi.discordClient.Logger.Log(LogLevel.Warning, new EventId(420, Assembly.GetEntryAssembly()?.GetName().Name), Message);
			}
			catch (NullReferenceException)
			{
				Console.WriteLine("[WARNING] " + Message);
			}
		}

		public static void Error(string Message)
		{
			try
			{
				SupportBoi.discordClient.Logger.Log(LogLevel.Error, new EventId(420, Assembly.GetEntryAssembly()?.GetName().Name), Message);
			}
			catch (NullReferenceException)
			{
				Console.WriteLine("[ERROR] " + Message);
			}
		}

		public static void Fatal(string Message)
		{
			try
			{
				SupportBoi.discordClient.Logger.Log(LogLevel.Critical, new EventId(420, Assembly.GetEntryAssembly()?.GetName().Name), Message);
			}
			catch (NullReferenceException)
			{
				Console.WriteLine("[CRITICAL] " + Message);
			}
		}
	}
}