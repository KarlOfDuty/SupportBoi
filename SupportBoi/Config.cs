using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json.Linq;
using SupportBoi.Properties;
using YamlDotNet.Serialization;

namespace SupportBoi
{
	internal static class Config
	{
		internal static string token = "";
		internal static ulong logChannel;
		internal static string welcomeMessage = "";
		internal static string logLevel = "Information";
		internal static string timestampFormat = "yyyy-MMM-dd HH:mm";
		internal static bool randomAssignment = false;
		internal static bool randomAssignRoleOverride = false;
		internal static string presenceType = "Playing";
		internal static string presenceText = "";
		internal static bool newCommandUsesSelector = false;
		internal static int ticketLimit = 5;

		internal static bool ticketUpdatedNotifications = false;
		internal static double ticketUpdatedNotificationDelay = 0.0;
		internal static bool assignmentNotifications = false;
		internal static bool closingNotifications = false;

		internal static string hostName = "127.0.0.1";
		internal static int port = 3306;
		internal static string database = "supportbot";
		internal static string username = "supportbot";
		internal static string password = "";
		
		public static void LoadConfig()
		{
			// Writes default config to file if it does not already exist
			if (!File.Exists("./config.yml"))
			{
				File.WriteAllText("./config.yml", Encoding.UTF8.GetString(Resources.default_config));
			}

			// Reads config contents into FileStream
			FileStream stream = File.OpenRead("./config.yml");

			// Converts the FileStream into a YAML object
			IDeserializer deserializer = new DeserializerBuilder().Build();
			object yamlObject = deserializer.Deserialize(new StreamReader(stream));

			// Converts the YAML object into a JSON object as the YAML ones do not support traversal or selection of nodes by name 
			ISerializer serializer = new SerializerBuilder().JsonCompatible().Build();
			JObject json = JObject.Parse(serializer.Serialize(yamlObject));

			// Sets up the bot
			token = json.SelectToken("bot.token")?.Value<string>() ?? "";
			logChannel = json.SelectToken("bot.log-channel")?.Value<ulong>() ?? 0;
			welcomeMessage = json.SelectToken("bot.welcome-message")?.Value<string>() ?? "";
			logLevel = json.SelectToken("bot.console-log-level")?.Value<string>() ?? "";
			timestampFormat = json.SelectToken("bot.timestamp-format")?.Value<string>() ?? "yyyy-MM-dd HH:mm";
			randomAssignment = json.SelectToken("bot.random-assignment")?.Value<bool>() ?? false;
			randomAssignRoleOverride = json.SelectToken("bot.random-assign-role-override")?.Value<bool>() ?? false;
			presenceType = json.SelectToken("bot.presence-type")?.Value<string>() ?? "Playing";
			presenceText = json.SelectToken("bot.presence-text")?.Value<string>() ?? "";
			newCommandUsesSelector = json.SelectToken("bot.new-command-uses-selector")?.Value<bool>() ?? false;
			ticketLimit =json.SelectToken("bot.ticket-limit")?.Value<int>() ?? 5;

			ticketUpdatedNotifications = json.SelectToken("notifications.ticket-updated")?.Value<bool>() ?? false;
			ticketUpdatedNotificationDelay = json.SelectToken("notifications.ticket-updated-delay")?.Value<double>() ?? 0.0;
			assignmentNotifications = json.SelectToken("notifications.assignment")?.Value<bool>() ?? false;
			closingNotifications = json.SelectToken("notifications.closing")?.Value<bool>() ?? false;

			// Reads database info
			hostName = json.SelectToken("database.address")?.Value<string>() ?? "";
			port = json.SelectToken("database.port")?.Value<int>() ?? 3306;
			database = json.SelectToken("database.name")?.Value<string>() ?? "supportbot";
			username = json.SelectToken("database.user")?.Value<string>() ?? "supportbot";
			password = json.SelectToken("database.password")?.Value<string>() ?? "";

			timestampFormat = timestampFormat.Trim();
		}
	}
}
