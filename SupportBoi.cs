using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;
using DSharpPlus;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using SupportBoi.Commands;
using CommandLine;

namespace SupportBoi;

internal static class SupportBoi
{
    // Sets up a dummy client to use for logging
    private static readonly DiscordConfiguration config = new()
    {
        Token = "DUMMY_TOKEN",
        TokenType = TokenType.Bot,
        MinimumLogLevel = LogLevel.Debug,
        AutoReconnect = true,
        Intents = DiscordIntents.All,
        LogTimestampFormat = "yyyy-MM-dd HH:mm:ss",
        LogUnknownEvents = false
    };

    public static DiscordClient discordClient { get; private set; } = new(config);

    private static SlashCommandsExtension commands = null;

    public class CommandLineArguments
    {
        [CommandLine.Option('c',
                            "config",
                            Required = false,
                            HelpText = "Select a config file to use.",
                            Default = "config.yml",
                            MetaValue = "PATH")]
        public string configPath { get; set; }

        [CommandLine.Option('t',
                            "transcripts",
                            Required = false,
                            HelpText = "Select directory to store transcripts in.",
                            Default = "./transcripts",
                            MetaValue = "PATH")]
        public string transcriptDir { get; set; }

        [CommandLine.Option(
            "leave",
            Required = false,
            HelpText = "Leaves one or more Discord servers. " +
                       "You can check which servers your bot is in when it starts up.",
            MetaValue = "ID,ID,ID...",
            Separator = ','
        )]
        public IEnumerable<ulong> serversToLeave { get; set; }
    }

    internal static CommandLineArguments commandLineArgs;

    private static void Main(string[] args)
    {
        StringWriter sw = new StringWriter();
        commandLineArgs = new Parser(settings =>
        {
            settings.AutoHelp = true;
            settings.HelpWriter = sw;
            settings.AutoVersion = false;
        }).ParseArguments<CommandLineArguments>(args).Value;

        // CommandLineParser has some bugs related to the built-in version option, ignore the output if it isn't found.
        if (!sw.ToString().Contains("Option 'version' is unknown."))
        {
            Console.Write(sw);
        }

        if (args.Contains("--help"))
        {
            return;
        }

        if (args.Contains("--version"))
        {
            Console.WriteLine(Assembly.GetEntryAssembly()?.GetName().Name + ' ' + GetVersion());
            Console.WriteLine("Build time: " + BuildInfo.BuildTimeUTC.ToString("yyyy-MM-dd HH:mm:ss") + " UTC");
            return;
        }

        MainAsync().GetAwaiter().GetResult();
    }

    private static async Task MainAsync()
    {
        Logger.Log("Starting " + Assembly.GetEntryAssembly()?.GetName().Name + " version " + GetVersion() + "...");
        try
        {
            Reload();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }
        catch (Exception e)
        {
            Logger.Fatal("Fatal error:\n" + e);
            Console.ReadLine();
        }
    }

    public static string GetVersion()
    {
        Version version = Assembly.GetEntryAssembly()?.GetName().Version;
        return version?.Major + "."
             + version?.Minor + "."
             + version?.Build
             + (version?.Revision == 0 ? "" : "-" + (char)(64 + version?.Revision ?? 0))
             + " (" + ThisAssembly.Git.Commit + ")";
    }

    public static async void Reload()
    {
        if (discordClient != null)
        {
            await discordClient.DisconnectAsync();
            discordClient.Dispose();
        }

        Config.LoadConfig();

        // Check if token is unset
        if (Config.token is "<add-token-here>" or "")
        {
            Logger.Fatal("You need to set your bot token in the config and start the bot again.");
            throw new ArgumentException("Invalid Discord bot token");
        }

        // Database connection and setup
        try
        {
            Logger.Log("Connecting to database... (" + Config.hostName + ":" + Config.port + ")");
            Database.SetConnectionString(Config.hostName, Config.port, Config.database, Config.username, Config.password);
            Database.SetupTables();
        }
        catch (Exception e)
        {
            Logger.Fatal("Could not set up database tables, please confirm connection settings, status of the server and permissions of MySQL user. Error: " + e);
            throw;
        }

        Logger.Log("Setting up Discord client...");

        // Setting up client configuration
        config.Token = Config.token;
        config.MinimumLogLevel = Config.logLevel;

        discordClient = new DiscordClient(config);

        Logger.Log("Hooking events...");
        discordClient.Ready += EventHandler.OnReady;
        discordClient.GuildAvailable += EventHandler.OnGuildAvailable;
        discordClient.ClientErrored += EventHandler.OnClientError;
        discordClient.MessageCreated += EventHandler.OnMessageCreated;
        discordClient.GuildMemberAdded += EventHandler.OnMemberAdded;
        discordClient.GuildMemberRemoved += EventHandler.OnMemberRemoved;
        discordClient.ComponentInteractionCreated += EventHandler.OnComponentInteractionCreated;

        discordClient.UseInteractivity(new InteractivityConfiguration
        {
            PaginationBehaviour = PaginationBehaviour.Ignore,
            PaginationDeletion = PaginationDeletion.DeleteMessage,
            Timeout = TimeSpan.FromMinutes(15)
        });

        Logger.Log("Registering commands...");
        commands = discordClient.UseSlashCommands();

        commands.RegisterCommands<AddCategoryCommand>();
        commands.RegisterCommands<AddCommand>();
        commands.RegisterCommands<AddMessageCommand>();
        commands.RegisterCommands<AddStaffCommand>();
        commands.RegisterCommands<AssignCommand>();
        commands.RegisterCommands<BlacklistCommand>();
        commands.RegisterCommands<CloseCommand>();
        commands.RegisterCommands<CreateButtonPanelCommand>();
        commands.RegisterCommands<CreateSelectionBoxPanelCommand>();
        commands.RegisterCommands<ListAssignedCommand>();
        commands.RegisterCommands<ListCommand>();
        commands.RegisterCommands<ListOpen>();
        commands.RegisterCommands<ListUnassignedCommand>();
        commands.RegisterCommands<MoveCommand>();
        commands.RegisterCommands<NewCommand>();
        commands.RegisterCommands<RandomAssignCommand>();
        commands.RegisterCommands<RemoveCategoryCommand>();
        commands.RegisterCommands<RemoveMessageCommand>();
        commands.RegisterCommands<RemoveStaffCommand>();
        commands.RegisterCommands<SayCommand>();
        commands.RegisterCommands<SetSummaryCommand>();
        commands.RegisterCommands<StatusCommand>();
        commands.RegisterCommands<SummaryCommand>();
        commands.RegisterCommands<ToggleActiveCommand>();
        commands.RegisterCommands<TranscriptCommand>();
        commands.RegisterCommands<UnassignCommand>();
        commands.RegisterCommands<UnblacklistCommand>();
        commands.RegisterCommands<AdminCommands>();

        Logger.Log("Hooking command events...");
        commands.SlashCommandErrored += EventHandler.OnCommandError;

        Logger.Log("Connecting to Discord...");
        await discordClient.ConnectAsync();
    }
}