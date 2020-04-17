using DSharpPlus.Entities;
using Newtonsoft.Json.Linq;
using SupportBoi.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.Serialization;

namespace SupportBoi
{
    internal static class Config
    {
        internal static string token = "";
        internal static string prefix = "";
        internal static ulong logChannel;
        internal static ulong ticketCategory;
        internal static ulong reactionMessage;
        internal static string welcomeMessage = "";
        internal static string logLevel = "Info";
        internal static string timestampFormat = "yyyy-MMM-dd HH:mm";
        internal static bool randomAssignment = false;
        internal static string presenceGame = "";

        internal static bool ticketUpdatedNotifications = false;
        internal static double ticketUpdatedNotificationDelay = 0.0;
        internal static bool assignmentNotifications = false;
        internal static bool closingNotifications = false;

        internal static string hostName = "127.0.0.1";
        internal static int port = 3306;
        internal static string database = "supportbot";
        internal static string username = "";
        internal static string password = "";

        private static readonly Dictionary<string, ulong[]> permissions = new Dictionary<string, ulong[]>
        {
			// Public commands
            { "new",                     new ulong[]{ } },
            { "close",                      new ulong[]{ } },
            { "transcript",                 new ulong[]{ } },
            { "status",                     new ulong[]{ } },
            { "summary",                    new ulong[]{ } },
            { "list",                       new ulong[]{ } },
			// Moderator commands
			{ "add",                     new ulong[]{ } },
            { "assign",                     new ulong[]{ } },
            { "rassign",                    new ulong[]{ } },
            { "unassign",                   new ulong[]{ } },
            { "blacklist",                  new ulong[]{ } },
            { "unblacklist",                new ulong[]{ } },
            { "setsummary",                 new ulong[]{ } },
            { "toggleactive",               new ulong[]{ } },
            { "listassigned",               new ulong[]{ } },
            { "listunassigned",             new ulong[]{ } },
            { "listoldest",                 new ulong[]{ } },
            { "move",                       new ulong[]{ } },
			// Admin commands
			{ "reload",                      new ulong[]{ } },
            { "setticket",                  new ulong[]{ } },
            { "unsetticket",                new ulong[]{ } },
            { "addstaff",                   new ulong[]{ } },
            { "removestaff",                new ulong[]{ } },
        };

        internal static bool sheetsEnabled = false;
        internal static string spreadsheetID = "";

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
            reactionMessage = json.SelectToken("bot.reaction-message").Value<ulong>();
            welcomeMessage = json.SelectToken("bot.welcome-message").Value<string>() ?? "";
            logLevel = json.SelectToken("bot.console-log-level").Value<string>() ?? "";
            timestampFormat = json.SelectToken("bot.timestamp-format").Value<string>() ?? "yyyy-MM-dd HH:mm";
            randomAssignment = json.SelectToken("bot.random-assignment").Value<bool>();
            presenceGame = json.SelectToken("bot.presence-game").Value<string>();

            ticketUpdatedNotifications = json.SelectToken("notifications.ticket-updated").Value<bool>();
            ticketUpdatedNotificationDelay = json.SelectToken("notifications.ticket-updated-delay").Value<double>();
            assignmentNotifications = json.SelectToken("notifications.assignment").Value<bool>();
            closingNotifications = json.SelectToken("notifications.closing").Value<bool>();

            // Reads database info
            hostName = json.SelectToken("database.address").Value<string>() ?? "";
            port = json.SelectToken("database.port").Value<int>();
            database = json.SelectToken("database.name").Value<string>() ?? "";
            username = json.SelectToken("database.user").Value<string>() ?? "";
            password = json.SelectToken("database.password").Value<string>() ?? "";

            timestampFormat = timestampFormat.Trim();

            foreach (KeyValuePair<string, ulong[]> node in permissions.ToList())
            {
                try
                {
                    permissions[node.Key] = json.SelectToken("permissions." + node.Key).Value<JArray>().Values<ulong>().ToArray();
                }
                catch (ArgumentNullException)
                {
                    Console.WriteLine("Permission node '" + node.Key + "' was not found in the config, using default value: []");
                }
            }

            sheetsEnabled = json.SelectToken("sheets.enabled").Value<bool>();
            spreadsheetID = json.SelectToken("sheets.id").Value<string>() ?? "";
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
    }
}
