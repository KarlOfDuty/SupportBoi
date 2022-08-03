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
		internal static ulong ticketCategory;
		internal static string welcomeMessage = "";
		internal static string logLevel = "Information";
		internal static string timestampFormat = "yyyy-MMM-dd HH:mm";
		internal static bool randomAssignment = false;
		internal static bool randomAssignRoleOverride = false;
		internal static string presenceType = "Playing";
		internal static string presenceText = "";

		internal static bool ticketUpdatedNotifications = false;
		internal static double ticketUpdatedNotificationDelay = 0.0;
		internal static bool assignmentNotifications = false;
		internal static bool closingNotifications = false;

		internal static string hostName = "127.0.0.1";
		internal static int port = 3306;
		internal static string database = "supportbot";
		internal static string username = "supportbot";
		internal static string password = "";

		private static readonly Dictionary<string, ulong[]> permissions = new Dictionary<string, ulong[]>
		{
			// Public commands
			{ "close",						new ulong[]{ } },
			{ "list",						new ulong[]{ } },
            { "new",						new ulong[]{ } },
			{ "say",                        new ulong[]{ } },
			{ "status",						new ulong[]{ } },
			{ "summary",					new ulong[]{ } },
            { "transcript",					new ulong[]{ } },
			// Moderator commands
			{ "add",						new ulong[]{ } },
			{ "addmessage",                 new ulong[]{ } },
			{ "assign",						new ulong[]{ } },
			{ "blacklist",					new ulong[]{ } },
			{ "listassigned",				new ulong[]{ } },
			{ "listoldest",					new ulong[]{ } },
			{ "listunassigned",				new ulong[]{ } },
			{ "move",						new ulong[]{ } },
			{ "rassign",					new ulong[]{ } },
			{ "removemessage",              new ulong[]{ } },
			{ "setsummary",					new ulong[]{ } },
			{ "toggleactive",				new ulong[]{ } },
			{ "unassign",					new ulong[]{ } },
			{ "unblacklist",				new ulong[]{ } },
			// Admin commands
			{ "addstaff",					new ulong[]{ } },
			{ "reload",						new ulong[]{ } },
			{ "removestaff",				new ulong[]{ } },
			{ "setticket",					new ulong[]{ } },
			{ "unsetticket",				new ulong[]{ } },
		};
		
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
			logChannel = json.SelectToken("bot.log-channel").Value<ulong>();
			ticketCategory = json.SelectToken("bot.ticket-category")?.Value<ulong>() ?? 0;
			welcomeMessage = json.SelectToken("bot.welcome-message").Value<string>() ?? "";
			logLevel = json.SelectToken("bot.console-log-level").Value<string>() ?? "";
			timestampFormat = json.SelectToken("bot.timestamp-format").Value<string>() ?? "yyyy-MM-dd HH:mm";
			randomAssignment = json.SelectToken("bot.random-assignment")?.Value<bool>() ?? false;
			randomAssignRoleOverride = json.SelectToken("bot.random-assign-role-override")?.Value<bool>() ?? false;
			presenceType = json.SelectToken("bot.presence-type")?.Value<string>() ?? "Playing";
			presenceText = json.SelectToken("bot.presence-text")?.Value<string>() ?? "";

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

			foreach (KeyValuePair<string, ulong[]> node in permissions.ToList())
			{
				try
				{
					permissions[node.Key] = json.SelectToken("permissions." + node.Key).Value<JArray>().Values<ulong>().ToArray();
				}
				catch (ArgumentNullException)
				{
					Logger.Warn(LogID.CONFIG, "Permission node '" + node.Key + "' was not found in the config, using default value: []");
				}
			}
		}

		/// <summary>
		/// Checks whether a user has a specific permission.
		/// </summary>
		/// <param name="member">The Discord user to check.</param>
		/// <param name="permission">The permission name to check.</param>
		/// <returns></returns>
		public static bool HasPermission(DiscordMember member, string permission)
		{
			return member.Roles.Any(role => permissions[permission].Contains(role.Id)) || permissions[permission].Contains(member.Guild.Id);
		}
		
		public class ConfigPermissionCheckAttribute : SlashCheckBaseAttribute
		{
			public string permissionName { get; }

			public ConfigPermissionCheckAttribute(string permission)
			{
				permissionName = permission;
			}
			
			public override async Task<bool> ExecuteChecksAsync(InteractionContext command)
			{
				try
				{
					// Check if the user has permission to use this command.
					if (!HasPermission(command.Member, permissionName))
					{
						return false;
					}

					return true;
				}
				catch (Exception e)
				{
					Logger.Error(LogID.COMMAND, "Exception occured: " + e.GetType() + ": " + e);
					await command.CreateResponseAsync(new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "Error occured when checking permissions, please report this to the developer."
					});
					return false;
				}
			}
		}
	}
}
