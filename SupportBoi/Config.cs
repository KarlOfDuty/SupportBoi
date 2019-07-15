using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DSharpPlus.Entities;
using Newtonsoft.Json.Linq;
using SupportBoi.Properties;
using YamlDotNet.Serialization;

namespace SupportBoi
{
	internal static class Config
	{
		internal static string token = "";
		internal static string prefix = "";
		internal static ulong logChannel;
		internal static ulong ticketCategory;
		internal static string welcomeMessage = "";
		internal static string logLevel = "Info";

		internal static string hostName = "127.0.0.1";
		internal static int port = 3306;
		internal static string database = "supportbot";
		internal static string username = "";
		internal static string password = "";

		internal static ulong adminRole = 0;
		internal static ulong moderatorRole = 0;

		internal static string timestampFormat = "yyyy-MMM-dd HH:mm";

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
			token = json.SelectToken("bot.token").Value<string>() ?? "";
			prefix = json.SelectToken("bot.prefix").Value<string>() ?? "";
			logChannel = json.SelectToken("bot.log-channel").Value<ulong>();
			ticketCategory = json.SelectToken("bot.ticket-category").Value<ulong>();
			welcomeMessage = json.SelectToken("bot.welcome-message").Value<string>() ?? "";
			logLevel = json.SelectToken("bot.console-log-level").Value<string>() ?? "";

			// Reads database info
			hostName = json.SelectToken("database.address").Value<string>() ?? "";
			port = json.SelectToken("database.port").Value<int>();
			database = json.SelectToken("database.name").Value<string>() ?? "";
			username = json.SelectToken("database.user").Value<string>() ?? "";
			password = json.SelectToken("database.password").Value<string>() ?? "";

			adminRole = json.SelectToken("permissions.admin-role").Value<ulong>();
			moderatorRole = json.SelectToken("permissions.moderator-role").Value<ulong>();

			timestampFormat = json.SelectToken("transcripts.timestamp-format").Value<string>() ?? "yyyy-MMM-dd HH:mm";
			timestampFormat = timestampFormat.Trim();
		}

		/// <summary>
		/// Checks whether a user has a moderator rank or higher in discord.
		/// </summary>
		/// <param name="roles">The user's roles.</param>
		/// <returns>True if the user has moderator access, false if not.</returns>
		public static bool IsModerator(IEnumerable<DiscordRole> roles)
		{
			return roles.Any(x => x.Id == Config.adminRole || x.Id == Config.moderatorRole);
		}

		/// <summary>
		/// Checks whether a user has an admin rank in discord.
		/// </summary>
		/// <param name="roles">The user's roles.</param>
		/// <returns>True if the user has admin access, false if not.</returns>
		public static bool IsAdmin(IEnumerable<DiscordRole> roles)
		{
			return roles.Any(x => x.Id == Config.adminRole);
		}
	}
}
