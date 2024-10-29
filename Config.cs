using System;
using System.IO;
using DSharpPlus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using YamlDotNet.Serialization;

namespace SupportBoi;

internal static class Config
{
    internal static string token = "";
    internal static ulong logChannel;
    internal static string welcomeMessage = "";
    internal static TimestampFormat timestampFormat = TimestampFormat.RelativeTime;
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

    private static string configPath = "./config.yml";

    public static void LoadConfig()
    {
        if (!string.IsNullOrEmpty(SupportBoi.commandLineArgs.configPath))
        {
            configPath = SupportBoi.commandLineArgs.configPath;
        }

        Logger.Log("Loading config \"" + Path.GetFullPath(configPath) + "\"");

        // Writes default config to file if it does not already exist
        if (!File.Exists(configPath))
        {
            File.WriteAllText(configPath, Utilities.ReadManifestData("default_config.yml"));
        }

        // Reads config contents into FileStream
        FileStream stream = File.OpenRead(configPath);

        // Converts the FileStream into a YAML object
        IDeserializer deserializer = new DeserializerBuilder().Build();
        object yamlObject = deserializer.Deserialize(new StreamReader(stream)) ?? "";

        // Converts the YAML object into a JSON object as the YAML ones do not support traversal or selection of nodes by name
        ISerializer serializer = new SerializerBuilder().JsonCompatible().Build();
        JObject json = JObject.Parse(serializer.Serialize(yamlObject));

        // Sets up the bot
        token = json.SelectToken("bot.token")?.Value<string>() ?? "";
        logChannel = json.SelectToken("bot.log-channel")?.Value<ulong>() ?? 0;
        welcomeMessage = json.SelectToken("bot.welcome-message")?.Value<string>() ?? "";
        string stringLogLevel = json.SelectToken("bot.console-log-level")?.Value<string>() ?? "";

        if (!Enum.TryParse(stringLogLevel, true, out LogLevel logLevel))
        {
            logLevel = LogLevel.Information;
            Logger.Warn("Log level '" + stringLogLevel + "' invalid, using 'Information' instead.");
        }
        Logger.SetLogLevel(logLevel);

        string stringTimestampFormat = json.SelectToken("bot.timestamp-format")?.Value<string>() ?? "RelativeTime";

        if (!Enum.TryParse(stringTimestampFormat, true, out timestampFormat))
        {
            timestampFormat = TimestampFormat.RelativeTime;
            Logger.Warn("Timestamp '" + stringTimestampFormat + "' invalid, using 'RelativeTime' instead.");
        }

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
    }
}