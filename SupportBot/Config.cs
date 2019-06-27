using System;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using SupportBot.Properties;
using YamlDotNet.Serialization;

namespace SupportBot
{
	internal static class Config
	{
		internal static string token = "";
		internal static string prefix = "";
		internal static ulong logChannel;
		internal static ulong ticketCategory;
		internal static string welcomeMessage = "";
		internal static string logLevel = "Info";

		internal static String hostName = "127.0.0.1";
		internal static int port = 3306;
		internal static String database = "supportbot";
		internal static String username = "";
		internal static String password = "";

		internal static ulong adminRole = 0;
		internal static ulong moderatorRole = 0;
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
			token = json.SelectToken("bot.token").Value<string>();
			prefix = json.SelectToken("bot.prefix").Value<string>();
			logChannel = json.SelectToken("bot.log-channel").Value<ulong>();
			ticketCategory = json.SelectToken("bot.ticket-category").Value<ulong>();
			welcomeMessage = json.SelectToken("bot.welcome-message").Value<string>();
			logLevel = json.SelectToken("bot.console-log-level").Value<string>();

			// Reads database info
			hostName = json.SelectToken("database.address").Value<string>();
			port = json.SelectToken("database.port").Value<int>();
			database = json.SelectToken("database.name").Value<string>();
			username = json.SelectToken("database.user").Value<string>();
			password = json.SelectToken("database.password").Value<string>();

			adminRole = json.SelectToken("permissions.admin-role").Value<ulong>();
			moderatorRole = json.SelectToken("permissions.moderator-role").Value<ulong>();
		}
	}
}
